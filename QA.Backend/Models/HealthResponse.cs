namespace QA.Backend.Models;

public sealed class HealthResponse
{
    public bool Success { get; init; } = true;
    public string Status { get; init; } = "Healthy";
    public string Service { get; init; } = "QA.Backend";
    public string Environment { get; init; } = string.Empty;
    public DateTimeOffset TimestampUtc { get; init; }
}
