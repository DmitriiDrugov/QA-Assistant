using Microsoft.Extensions.Options;
using QA.Backend.Models;
using QA.Backend.Options;

namespace QA.Backend.Services;

public sealed class QaService(
    KnowledgeBaseService knowledgeBaseService,
    SearchService searchService,
    IAiService aiService,
    IOptions<KnowledgeBaseOptions> knowledgeBaseOptions)
{
    private readonly KnowledgeBaseService _knowledgeBaseService = knowledgeBaseService;
    private readonly SearchService _searchService = searchService;
    private readonly IAiService _aiService = aiService;
    private readonly KnowledgeBaseOptions _knowledgeBaseOptions = knowledgeBaseOptions.Value;

    public async Task<AskQuestionResponse> AskQuestionAsync(string question, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            throw new QaValidationException("Question is required.");
        }

        var trimmedQuestion = question.Trim();
        if (trimmedQuestion.Length > _knowledgeBaseOptions.MaxQuestionLength)
        {
            throw new QaValidationException($"Question is too long. Maximum length is {_knowledgeBaseOptions.MaxQuestionLength} characters.");
        }

        var chunks = await _knowledgeBaseService.GetChunksAsync(cancellationToken);
        var matchedChunk = _searchService.FindMostRelevantChunk(chunks, trimmedQuestion);
        var answer = await _aiService.GenerateAnswerAsync(trimmedQuestion, matchedChunk, cancellationToken);

        return new AskQuestionResponse
        {
            Success = true,
            Question = trimmedQuestion,
            Answer = answer,
            MatchedChunk = matchedChunk,
            Source = "knowledge_base"
        };
    }
}
