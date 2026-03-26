namespace QA.Backend.Services;

public sealed class QaValidationException(string message) : Exception(message);

public sealed class KnowledgeBaseException : Exception
{
    public KnowledgeBaseException(string message)
        : base(message)
    {
    }

    public KnowledgeBaseException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

public sealed class SearchException(string message) : Exception(message);

public sealed class AiProviderException : Exception
{
    public AiProviderException(string message)
        : base(message)
    {
    }

    public AiProviderException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

public sealed class ConversationNotFoundException(string conversationId)
    : Exception($"Conversation '{conversationId}' was not found.");
