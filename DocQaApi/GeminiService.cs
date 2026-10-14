using System.Net.Http.Json;
using System.Text.Json;

namespace DocQaApi;

public interface IAiService
{
    Task<string> AskAsync(string question);
}

public class GeminiService : IAiService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _model;

    public GeminiService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _apiKey = config["Gemini:ApiKey"]
            ?? throw new InvalidOperationException("Gemini:ApiKey is missing. Check User Secrets.");
        _model = config["Gemini:Model"] ?? "gemini-3.5-flash-lite";
    }

    public async Task<string> AskAsync(string question)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent";

        var body = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = question } } }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("x-goog-api-key", _apiKey);
        request.Content = JsonContent.Create(body);

        var response = await _http.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Gemini error {(int)response.StatusCode}: {json}");

        using var doc = JsonDocument.Parse(json);
        return doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString() ?? "";
    }
}