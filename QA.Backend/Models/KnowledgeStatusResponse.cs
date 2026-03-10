namespace QA.Backend.Models;

public sealed class KnowledgeStatusResponse
{
    public bool Success { get; init; } = true;
    public bool IsLoaded { get; init; }
    public int ChunkCount { get; init; }
    public string ConfiguredFilePath { get; init; } = string.Empty;
    public string? ResolvedFilePath { get; init; }
    public DateTimeOffset? LastLoadedUtc { get; init; }
}
