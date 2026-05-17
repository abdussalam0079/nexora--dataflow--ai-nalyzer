using System.Text.Json;
using DataFlow.Core.Models;
using DataFlow.WebApi.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DataFlow.WebApi.Controllers;

[ApiController]
[Route("api/v1/dashboards")]
public class DashboardsController(AppDbContext db) : ControllerBase
{
    [HttpGet("project/{projectId:int}")]
    public async Task<IActionResult> List(int projectId)
    {
        var list = await db.Dashboards
            .Where(d => d.ProjectId == projectId)
            .OrderByDescending(d => d.UpdatedAt)
            .ToListAsync();
        return Ok(list.Select(ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var d = await db.Dashboards.FindAsync(id);
        if (d == null) return NotFound();
        return Ok(ToDto(d));
    }

    [HttpPost("")]
    public async Task<IActionResult> Create([FromBody] JsonElement body)
    {
        var d = new DashboardEntity
        {
            ProjectId   = body.GetProperty("project_id").GetInt32(),
            DatasetId   = body.TryGetProperty("dataset_id", out var did) && did.ValueKind != JsonValueKind.Null ? did.GetInt32() : null,
            Name        = body.GetProperty("name").GetString() ?? "Dashboard",
            Description = body.TryGetProperty("description", out var desc) && desc.ValueKind != JsonValueKind.Null ? desc.GetString() : null,
            Scheme      = body.TryGetProperty("scheme", out var sc) ? sc.GetString() ?? "Metric Flow" : "Metric Flow",
            LayoutJson  = body.TryGetProperty("layout", out var lay) && lay.ValueKind != JsonValueKind.Null ? lay.GetRawText() : null,
            CreatedAt   = DateTime.UtcNow,
            UpdatedAt   = DateTime.UtcNow
        };
        db.Dashboards.Add(d);
        var proj = await db.Projects.FindAsync(d.ProjectId);
        if (proj != null) proj.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(ToDto(d));
    }

    [HttpPatch("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] JsonElement body)
    {
        var d = await db.Dashboards.FindAsync(id);
        if (d == null) return NotFound();
        if (body.TryGetProperty("name",   out var n) && n.ValueKind == JsonValueKind.String) d.Name   = n.GetString()!;
        if (body.TryGetProperty("scheme", out var s) && s.ValueKind == JsonValueKind.String) d.Scheme = s.GetString()!;
        if (body.TryGetProperty("layout", out var l) && l.ValueKind != JsonValueKind.Null)   d.LayoutJson = l.GetRawText();
        if (body.TryGetProperty("is_pinned", out var ip) && ip.ValueKind == JsonValueKind.True || ip.ValueKind == JsonValueKind.False)
            d.IsPinned = ip.GetBoolean();
        d.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(ToDto(d));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var d = await db.Dashboards.FindAsync(id);
        if (d == null) return NotFound();
        db.Dashboards.Remove(d);
        await db.SaveChangesAsync();
        return Ok(new { detail = "deleted" });
    }

    private static object ToDto(DashboardEntity d) => new
    {
        d.Id, d.ProjectId, d.DatasetId,
        d.Name, d.Description, d.Scheme,
        layout_json = d.LayoutJson,
        layout      = d.LayoutJson != null ? (object?)JsonDocument.Parse(d.LayoutJson).RootElement : null,
        d.IsPinned, d.CreatedAt, d.UpdatedAt
    };
}
