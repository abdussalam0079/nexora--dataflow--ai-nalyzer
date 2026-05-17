using System.Text.Json;
using DataFlow.WebApi.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DataFlow.WebApi.Controllers;

[ApiController]
[Route("api/v1/history")]
public class ChatHistoryController(AppDbContext db) : ControllerBase
{
    // ── Sessions ──────────────────────────────────────────────────
    [HttpGet("sessions/{projectId:int}")]
    public async Task<IActionResult> ListSessions(int projectId)
    {
        var sessions = await db.ChatSessions
            .Where(s => s.ProjectId == projectId)
            .OrderByDescending(s => s.UpdatedAt)
            .Select(s => new
            {
                s.Id, s.ProjectId, s.DatasetId, s.Title, s.SessionId,
                s.CreatedAt, s.UpdatedAt,
                message_count = s.Messages.Count
            }).ToListAsync();
        return Ok(sessions);
    }

    [HttpPost("sessions")]
    public async Task<IActionResult> CreateSession([FromBody] JsonElement body)
    {
        var s = new ChatSessionEntity
        {
            ProjectId = body.TryGetProperty("project_id", out var pid) ? pid.GetInt32() : 0,
            DatasetId = body.TryGetProperty("dataset_id", out var did) && did.ValueKind != JsonValueKind.Null ? did.GetInt32() : null,
            Title     = body.TryGetProperty("title", out var t) ? t.GetString() : null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.ChatSessions.Add(s);
        await db.SaveChangesAsync();
        return Ok(new { s.Id, s.ProjectId, s.DatasetId, s.Title, s.SessionId, s.CreatedAt, s.UpdatedAt, message_count = 0 });
    }

    [HttpPatch("sessions/{id:int}/title")]
    public async Task<IActionResult> UpdateTitle(int id, [FromQuery] string title)
    {
        var s = await db.ChatSessions.FindAsync(id);
        if (s == null) return NotFound();
        s.Title = title;
        s.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(new { s.Id, s.Title });
    }

    [HttpDelete("sessions/{id:int}")]
    public async Task<IActionResult> DeleteSession(int id)
    {
        var s = await db.ChatSessions.FindAsync(id);
        if (s == null) return NotFound();
        db.ChatSessions.Remove(s);
        await db.SaveChangesAsync();
        return Ok(new { detail = "deleted" });
    }

    // ── Messages ──────────────────────────────────────────────────
    [HttpGet("sessions/{sessionId:int}/messages")]
    public async Task<IActionResult> GetMessages(int sessionId)
    {
        var msgs = await db.ChatMessages
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new { m.Id, m.SessionId, m.Role, m.Content, m.HasChart, m.ChartJson, m.CreatedAt })
            .ToListAsync();
        return Ok(msgs);
    }

    [HttpPost("sessions/{sessionId:int}/messages")]
    public async Task<IActionResult> AddMessage(int sessionId, [FromBody] JsonElement body)
    {
        var msg = new ChatMessageEntity
        {
            SessionId = sessionId,
            Role      = body.TryGetProperty("role",    out var r) ? r.GetString() ?? "user" : "user",
            Content   = body.TryGetProperty("content", out var c) ? c.GetString() ?? "" : "",
            CreatedAt = DateTime.UtcNow
        };
        db.ChatMessages.Add(msg);

        var session = await db.ChatSessions.FindAsync(sessionId);
        if (session != null) session.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Ok(new { msg.Id, msg.SessionId, msg.Role, msg.Content, msg.CreatedAt });
    }
}
