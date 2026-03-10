namespace QA.Backend.Options;

public sealed class AiOptions
{
    public const string SectionName = "Ai";

    public string Provider { get; set; } = "OpenAI";
    public string BaseUrl { get; set; } = "https://api.openai.com";
    public string ChatCompletionsPath { get; set; } = "/v1/chat/completions";
    public string Model { get; set; } = "gpt-4o-mini";
    public string ApiKey { get; set; } = string.Empty;
    public string SystemPrompt { get; set; } = "You are an IT support assistant. Answer using the provided context when possible.";
    public int TimeoutSeconds { get; set; } = 60;
}
