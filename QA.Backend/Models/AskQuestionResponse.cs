namespace QA.Backend.Models;

public sealed class AskQuestionResponse
{
    public bool Success { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public string MatchedChunk { get; set; } = string.Empty;
    public string Source { get; set; } = "knowledge_base";
}
