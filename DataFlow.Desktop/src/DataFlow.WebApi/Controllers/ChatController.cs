using System.Text.Json;
using DataFlow.WebApi.Data;
using DataFlow.WebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DataFlow.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/chat")]
public class ChatController(GroqChatService groq, AppDbContext db) : ControllerBase
{
    [HttpPost("")]
    [RequestSizeLimit(100 * 1024 * 1024)]
    public async Task<IActionResult> Chat(
        [FromForm] string message,
        [FromForm] string? session_id,
        [FromForm] string? history,
        IFormFile? file,
        CancellationToken ct)
    {
        // Parse history
        var historyList = new List<(string role, string content)>();
        if (!string.IsNullOrEmpty(history))
        {
            try
            {
                var arr = JsonSerializer.Deserialize<List<JsonElement>>(history);
                if (arr != null)
                    foreach (var item in arr)
                        historyList.Add((
                            item.GetProperty("role").GetString() ?? "user",
                            item.GetProperty("content").GetString() ?? ""
                        ));
            }
            catch { /* ignore bad history */ }
        }

        // Build system prompt
        var systemPrompt = BuildSystemPrompt(file, session_id, db);

        // If file uploaded, parse it and add to prompt
        string? newSessionId = session_id;
        if (file != null && file.Length > 0)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var tmpPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{ext}");
            try
            {
                await using (var fs = System.IO.File.Create(tmpPath))
                    await file.CopyToAsync(fs, ct);

                var parsed = FileParserService.Parse(tmpPath);
                var preview = BuildDataPreview(parsed, file.FileName);
                systemPrompt = $"You are a data analyst AI. The user uploaded a file.\n\n{preview}\n\nAnswer questions about this data.";
                newSessionId = Guid.NewGuid().ToString("N")[..12];
            }
            catch (Exception ex)
            {
                systemPrompt = $"You are a data analyst AI. File parse failed: {ex.Message}";
            }
            finally
            {
                if (System.IO.File.Exists(tmpPath)) System.IO.File.Delete(tmpPath);
            }
        }

        var answer = await groq.ChatAsync(message, systemPrompt, historyList, ct);

        return Ok(new
        {
            status = "ok",
            answer,
            session_id = newSessionId,
            source = "groq"
        });
    }

    private static string BuildSystemPrompt(IFormFile? file, string? sessionId, AppDbContext db)
    {
        return "You are DataFlow AI, an expert data analyst assistant. " +
               "Help users analyze data, create visualizations, find insights, and answer questions about their datasets. " +
               "Be concise, accurate, and helpful. Format responses with markdown when appropriate.";
    }

    private static string BuildDataPreview(ParsedFile parsed, string fileName)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"**File:** {fileName}");
        sb.AppendLine($"**Rows:** {parsed.Rows.Count:N0}  |  **Columns:** {parsed.Headers.Count}");
        sb.AppendLine($"**Columns:** {string.Join(", ", parsed.Headers)}");
        sb.AppendLine();

        // Sample rows
        sb.AppendLine("**Sample data (first 5 rows):**");
        sb.AppendLine("| " + string.Join(" | ", parsed.Headers) + " |");
        sb.AppendLine("| " + string.Join(" | ", parsed.Headers.Select(_ => "---")) + " |");
        foreach (var row in parsed.Rows.Take(5))
        {
            var vals = parsed.Headers.Select(h => row.GetValueOrDefault(h)?.ToString() ?? "");
            sb.AppendLine("| " + string.Join(" | ", vals) + " |");
        }

        // Basic stats for numeric columns
        var numericCols = parsed.Headers.Where(h =>
            parsed.Rows.Take(10).Any(r => r.GetValueOrDefault(h) is double)).ToList();
        if (numericCols.Any())
        {
            sb.AppendLine();
            sb.AppendLine("**Numeric column stats:**");
            foreach (var col in numericCols.Take(5))
            {
                var vals = parsed.Rows.Select(r => r.GetValueOrDefault(col)).OfType<double>().ToList();
                if (vals.Count == 0) continue;
                sb.AppendLine($"- {col}: min={vals.Min():F2}, max={vals.Max():F2}, avg={vals.Average():F2}");
            }
        }

        return sb.ToString();
    }
}
