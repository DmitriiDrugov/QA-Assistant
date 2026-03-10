namespace QA.Backend.Services;

public interface IAiService
{
    Task<string> GenerateAnswerAsync(string question, string context, CancellationToken cancellationToken = default);
}
