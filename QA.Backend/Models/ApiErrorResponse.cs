namespace QA.Backend.Models;

public sealed class ApiErrorResponse
{
    public bool Success { get; init; } = false;
    public string Message { get; init; } = string.Empty;
    public string? Details { get; init; }
}
