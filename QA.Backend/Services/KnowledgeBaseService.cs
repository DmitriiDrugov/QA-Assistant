using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;
using QA.Backend.Models;
using QA.Backend.Options;

namespace QA.Backend.Services;

public sealed class KnowledgeBaseService(
    IOptions<KnowledgeBaseOptions> knowledgeBaseOptions,
    IWebHostEnvironment environment,
    ILogger<KnowledgeBaseService> logger)
{
    private readonly KnowledgeBaseOptions _options = knowledgeBaseOptions.Value;
    private readonly IWebHostEnvironment _environment = environment;
    private readonly ILogger<KnowledgeBaseService> _logger = logger;
    private readonly SemaphoreSlim _loadLock = new(1, 1);

    private IReadOnlyList<string> _chunks = [];
    private bool _isLoaded;
    private string? _resolvedFilePath;
    private DateTimeOffset? _lastLoadedUtc;

    public async Task<IReadOnlyList<string>> GetChunksAsync(CancellationToken cancellationToken = default)
    {
        if (_isLoaded && _chunks.Count > 0)
        {
            return _chunks;
        }

        await LoadKnowledgeBaseAsync(forceReload: false, cancellationToken);
        return _chunks;
    }

    public KnowledgeStatusResponse GetStatus()
    {
        return new KnowledgeStatusResponse
        {
            IsLoaded = _isLoaded,
            ChunkCount = _chunks.Count,
            ConfiguredFilePath = _options.FilePath,
            ResolvedFilePath = _resolvedFilePath,
            LastLoadedUtc = _lastLoadedUtc
        };
    }

    public async Task<KnowledgeStatusResponse> ReloadAsync(CancellationToken cancellationToken = default)
    {
        await LoadKnowledgeBaseAsync(forceReload: true, cancellationToken);
        return GetStatus();
    }

    private async Task LoadKnowledgeBaseAsync(bool forceReload, CancellationToken cancellationToken)
    {
        await _loadLock.WaitAsync(cancellationToken);
        try
        {
            if (!forceReload && _isLoaded && _chunks.Count > 0)
            {
                return;
            }

            if (_options.ChunkSize <= 0)
            {
                throw new KnowledgeBaseException("Knowledge base chunk size must be greater than 0.");
            }

            if (_options.MaxQuestionLength <= 0)
            {
                throw new KnowledgeBaseException("Knowledge base max question length must be greater than 0.");
            }

            var resolvedPath = ResolveFilePath(_options.FilePath);
            if (string.IsNullOrWhiteSpace(resolvedPath) || !File.Exists(resolvedPath))
            {
                throw new KnowledgeBaseException("Knowledge base file was not found. Check KnowledgeBase:FilePath in configuration.");
            }

            string text;
            try
            {
                text = await File.ReadAllTextAsync(resolvedPath, cancellationToken);
            }
            catch (Exception ex)
            {
                throw new KnowledgeBaseException("Knowledge base file could not be read.", ex);
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new KnowledgeBaseException("Knowledge base file is empty.");
            }

            // Prefer logical sections first so retrieval keeps related facts together.
            var chunks = SplitIntoChunks(text, _options.ChunkSize);
            if (chunks.Count == 0)
            {
                throw new KnowledgeBaseException("No chunks were created from the knowledge base file.");
            }

            _chunks = chunks;
            _isLoaded = true;
            _resolvedFilePath = resolvedPath;
            _lastLoadedUtc = DateTimeOffset.UtcNow;

            _logger.LogInformation("Knowledge base loaded from {Path}. Chunks: {ChunkCount}", resolvedPath, chunks.Count);
        }
        finally
        {
            _loadLock.Release();
        }
    }

    private string? ResolveFilePath(string configuredFilePath)
    {
        if (string.IsNullOrWhiteSpace(configuredFilePath))
        {
            return null;
        }

        if (Path.IsPathRooted(configuredFilePath))
        {
            return configuredFilePath;
        }

        var candidates = new List<string>
        {
            Path.GetFullPath(Path.Combine(_environment.ContentRootPath, configuredFilePath)),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), configuredFilePath))
        };

        var parentDirectory = Directory.GetParent(_environment.ContentRootPath);
        if (parentDirectory is not null)
        {
            candidates.Add(Path.GetFullPath(Path.Combine(parentDirectory.FullName, configuredFilePath)));
        }

        foreach (var candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return candidates[0];
    }

    private static IReadOnlyList<string> SplitIntoChunks(string text, int chunkSize)
    {
        var normalized = text.Replace("\r\n", "\n");
        var sectionChunks = normalized
            .Split("\n---\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(section => section.Trim())
            .Where(section => !string.IsNullOrWhiteSpace(section))
            .ToList();

        if (sectionChunks.Count > 0)
        {
            return MergeOversizedSections(sectionChunks, chunkSize);
        }

        var chunks = new List<string>();

        for (var index = 0; index < normalized.Length; index += chunkSize)
        {
            var length = Math.Min(chunkSize, normalized.Length - index);
            chunks.Add(normalized.Substring(index, length).Trim());
        }

        return chunks.Where(chunk => !string.IsNullOrWhiteSpace(chunk)).ToList();
    }

    private static IReadOnlyList<string> MergeOversizedSections(IReadOnlyList<string> sections, int chunkSize)
    {
        var chunks = new List<string>();

        foreach (var section in sections)
        {
            if (section.Length <= chunkSize)
            {
                chunks.Add(section);
                continue;
            }

            // Fallback for very large sections: split on paragraph boundaries first, then hard-cut.
            var paragraphs = Regex.Split(section, @"\n\s*\n")
                .Where(paragraph => !string.IsNullOrWhiteSpace(paragraph))
                .ToList();

            if (paragraphs.Count == 0)
            {
                chunks.AddRange(HardSplit(section, chunkSize));
                continue;
            }

            var current = string.Empty;
            foreach (var paragraph in paragraphs)
            {
                var candidate = string.IsNullOrWhiteSpace(current) ? paragraph.Trim() : $"{current}\n\n{paragraph.Trim()}";
                if (candidate.Length <= chunkSize)
                {
                    current = candidate;
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(current))
                {
                    chunks.Add(current);
                }

                if (paragraph.Length <= chunkSize)
                {
                    current = paragraph.Trim();
                }
                else
                {
                    chunks.AddRange(HardSplit(paragraph.Trim(), chunkSize));
                    current = string.Empty;
                }
            }

            if (!string.IsNullOrWhiteSpace(current))
            {
                chunks.Add(current);
            }
        }

        return chunks;
    }

    private static IReadOnlyList<string> HardSplit(string text, int chunkSize)
    {
        var chunks = new List<string>();
        for (var index = 0; index < text.Length; index += chunkSize)
        {
            var length = Math.Min(chunkSize, text.Length - index);
            chunks.Add(text.Substring(index, length).Trim());
        }

        return chunks;
    }
}
