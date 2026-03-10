using Microsoft.Extensions.Options;
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

            // STEP 4: Move console chunking logic into a backend service.
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
        var chunks = new List<string>();

        for (var index = 0; index < text.Length; index += chunkSize)
        {
            var length = Math.Min(chunkSize, text.Length - index);
            chunks.Add(text.Substring(index, length));
        }

        return chunks;
    }
}
