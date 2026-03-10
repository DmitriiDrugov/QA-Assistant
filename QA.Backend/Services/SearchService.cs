namespace QA.Backend.Services;

public sealed class SearchService
{
    // STEP 5: Keep the original keyword matching behavior from the console prototype.
    public string FindMostRelevantChunk(IReadOnlyList<string> chunks, string question)
    {
        if (chunks.Count == 0)
        {
            throw new SearchException("No knowledge base chunks are available for search.");
        }

        var keywords = question
            .ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var bestChunk = string.Empty;
        var highestScore = 0;

        foreach (var chunk in chunks)
        {
            var score = 0;
            var lowerChunk = chunk.ToLowerInvariant();

            foreach (var keyword in keywords)
            {
                if (lowerChunk.Contains(keyword, StringComparison.Ordinal))
                {
                    score++;
                }
            }

            if (score > highestScore)
            {
                highestScore = score;
                bestChunk = chunk;
            }
        }

        if (string.IsNullOrWhiteSpace(bestChunk))
        {
            throw new SearchException("No relevant knowledge base chunk was found for the question.");
        }

        return bestChunk;
    }
}
