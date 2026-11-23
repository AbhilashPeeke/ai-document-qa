using System.Net.Http.Json;
using System.Text.Json;

namespace DocQaApi;

public interface IEmbeddingService
{
    Task<List<float[]>> EmbedDocumentsAsync(List<string> texts);
    Task<float[]> EmbedQueryAsync(string text);
}

public class GeminiEmbeddingService : IEmbeddingService
{
    private const int BatchSize = 50;
    private const int Dimensions = 768;

    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _model;

    public GeminiEmbeddingService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _apiKey = config["Gemini:ApiKey"]
            ?? throw new InvalidOperationException("Gemini:ApiKey is missing. Check User Secrets.");
        _model = config["Gemini:EmbeddingModel"] ?? "gemini-embedding-001";
    }

    public async Task<List<float[]>> EmbedDocumentsAsync(List<string> texts)
    {
        var all = new List<float[]>();
        for (int i = 0; i < texts.Count; i += BatchSize)
        {
            var batch = texts.Skip(i).Take(BatchSize).ToList();
            all.AddRange(await EmbedBatchAsync(batch, "RETRIEVAL_DOCUMENT"));
        }
        return all;
    }

    public async Task<float[]> EmbedQueryAsync(string text)
    {
        var result = await EmbedBatchAsync(new List<string> { text }, "RETRIEVAL_QUERY");
        return result[0];
    }

    private async Task<List<float[]>> EmbedBatchAsync(List<string> texts, string taskType)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:batchEmbedContents";

        var body = new
        {
            requests = texts.Select(t => new
            {
                model = $"models/{_model}",
                content = new { parts = new[] { new { text = t } } },
                taskType,
                outputDimensionality = Dimensions
            }).ToList()
        };

        for (int attempt = 1; attempt <= 4; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("x-goog-api-key", _apiKey);
            request.Content = JsonContent.Create(body);

            var response = await _http.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            // Free tier limit hit: wait and try again
            if ((int)response.StatusCode == 429 && attempt < 4)
            {
                await Task.Delay(TimeSpan.FromSeconds(15 * attempt));
                continue;
            }

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Embedding error {(int)response.StatusCode}: {json}");

            using var doc = JsonDocument.Parse(json);
            return doc.RootElement
                .GetProperty("embeddings")
                .EnumerateArray()
                .Select(e => e.GetProperty("values")
                              .EnumerateArray()
                              .Select(v => v.GetSingle())
                              .ToArray())
                .ToList();
        }

        throw new HttpRequestException("Embedding failed after retries.");
    }
}