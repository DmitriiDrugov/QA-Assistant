using System;
using System.IO;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        Console.WriteLine(" IT Support Q&A Application\n");

        string filePath = "knowledge_base.txt";
        if (!File.Exists(filePath))
        {
            Console.WriteLine("Error: Knowledge base file not found.");
            return;
        }

        string documentText = File.ReadAllText(filePath);
        Console.WriteLine("Knowledge base loaded successfully.");
        Console.WriteLine($"Characters loaded: {documentText.Length}");

        var chunks = SplitIntoChunks(documentText, 800);
        Console.WriteLine($"Total chunks created: {chunks.Count}");

        Console.WriteLine("\nAsk your IT question:");
        string userQuestion = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(userQuestion))
        {
            Console.WriteLine("Question cannot be empty.");
            return;
        }

        
        string relevantChunk = FindMostRelevantChunk(chunks, userQuestion);
        Console.WriteLine("\nMost relevant knowledge base section:");
        Console.WriteLine(relevantChunk);

        
        string answer = await AskOpenAI(userQuestion, relevantChunk);
        Console.WriteLine("\nAI Answer:\n" + answer);
    }

    
    static string FindMostRelevantChunk(List<string> chunks, string question)
    {
        string[] keywords = question
            .ToLower()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        string bestChunk = "";
        int highestScore = 0;

        foreach (var chunk in chunks)
        {
            int score = 0;
            string lowerChunk = chunk.ToLower();

            foreach (var keyword in keywords)
            {
                if (lowerChunk.Contains(keyword))
                    score++;
            }

            if (score > highestScore)
            {
                highestScore = score;
                bestChunk = chunk;
            }
        }

        return bestChunk;
    }

   
    static List<string> SplitIntoChunks(string text, int chunkSize)
    {
        List<string> chunks = new List<string>();

        for (int i = 0; i < text.Length; i += chunkSize)
        {
            int length = Math.Min(chunkSize, text.Length - i);
            chunks.Add(text.Substring(i, length));
        }

        return chunks;
    }

   
    static async Task<string> AskOpenAI(string question, string context)
    {
        string apiKey = "here we have to replace with your real API key";
        string endpoint = "https://api.openai.com/v1/chat/completions";

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        var requestBody = new
        {
            model = "gpt-3.5-turbo",
            messages = new[]
            {
                new { role = "system", content = "You are an IT support assistant." },
                new { role = "user", content = $"Context:\n{context}\n\nQuestion:\n{question}" }
            }
        };

        try
        {
            var response = await client.PostAsJsonAsync(endpoint, requestBody);

            if (!response.IsSuccessStatusCode)
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                return $"OpenAI API Error:\nStatus code: {response.StatusCode}\n{errorContent}";
            }

            var json = await response.Content.ReadFromJsonAsync<dynamic>();
            return json.choices[0].message.content.ToString();
        }
        catch (Exception ex)
        {
            return $"Exception calling OpenAI API: {ex.Message}";
        }
    }
}
