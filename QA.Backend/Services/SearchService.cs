namespace QA.Backend.Services;

public sealed class SearchService
{
    // STEP 5: Keep the original keyword matching behavior from the console prototype.
    public string FindMostRelevantChunk(IReadOnlyList<string> chunks, string question)
    {
        var match = TryFindMostRelevantChunk(chunks, question);
        if (match is null)
        {
            throw new SearchException("No relevant knowledge base chunk was found for the question.");
        }

        return match;
    }

    public string? TryFindMostRelevantChunk(IReadOnlyList<string> chunks, string question)
    {
        if (chunks.Count == 0)
        {
            throw new SearchException("No knowledge base chunks are available for search.");
        }

        var keywords = ExtractKeywords(question);

        var bestChunk = string.Empty;
        var highestScore = int.MinValue;

        foreach (var chunk in chunks)
        {
            var score = 0;
            var lowerChunk = chunk.ToLowerInvariant();

            foreach (var keyword in keywords)
            {
                if (keyword.Length >= 4 && lowerChunk.Contains(keyword, StringComparison.Ordinal))
                {
                    score += 3;
                    continue;
                }

                if (lowerChunk.Contains(keyword, StringComparison.Ordinal))
                {
                    score += 1;
                }
            }

            // Favor chunks whose title or keyword line directly matches the question terms.
            var header = chunk.Split('\n', 3)[0].ToLowerInvariant();
            if (keywords.Any(keyword => header.Contains(keyword, StringComparison.Ordinal)))
            {
                score += 5;
            }

            if (score > highestScore)
            {
                highestScore = score;
                bestChunk = chunk;
            }
        }

        if (string.IsNullOrWhiteSpace(bestChunk) || highestScore <= 0)
        {
            return null;
        }

        return bestChunk;
    }

    private static IReadOnlyList<string> ExtractKeywords(string question)
    {
        return question
            .ToLowerInvariant()
            .Split(new[] { ' ', '\t', '\r', '\n', ',', '.', ':', ';', '!', '?', '-', '(', ')', '"', '\'' },
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct()
            .ToList();
    }
}
