using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace DataFlow.WebApi.Services;

public class GroqChatService(IHttpClientFactory factory, IConfiguration config)
{
    private static readonly JsonSerializerOptions _opts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<string> ChatAsync(
        string userMessage,
        string? systemPrompt = null,
        List<(string role, string content)>? history = null,
        CancellationToken ct = default)
    {
        var apiKey = config["Groq:ApiKey"] ?? Environment.GetEnvironmentVariable("GROQ_API_KEY") ?? "";
        if (string.IsNullOrEmpty(apiKey))
            return "⚠ GROQ_API_KEY is not set. Set it via environment variables (e.g., GROQ_API_KEY) or a local .env file.";

        var messages = new List<object>();
        if (!string.IsNullOrEmpty(systemPrompt))
            messages.Add(new { role = "system", content = systemPrompt });

        if (history != null)
            foreach (var (role, content) in history.TakeLast(10))
                messages.Add(new { role, content });

        messages.Add(new { role = "user", content = userMessage });

        var body = new
        {
            model = "llama-3.3-70b-versatile",
            messages,
            max_tokens = 2048,
            temperature = 0.7
        };

        var http = factory.CreateClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var payload = JsonSerializer.Serialize(body);
        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };

        try
        {
            using var resp = await http.SendAsync(req, ct);
            var json = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
                return $"⚠ Groq API error {(int)resp.StatusCode}: {json}";

            using var doc = JsonDocument.Parse(json);
            return doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "No response.";
        }
        catch (Exception ex)
        {
            return $"⚠ Request failed: {ex.Message}";
        }
    }
}
