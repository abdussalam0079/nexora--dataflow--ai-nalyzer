using System.Text.Json;
using DataFlow.WebApi.Data;
using DataFlow.WebApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DataFlow.WebApi.Controllers;

[ApiController]
[Route("api/v1/datasets")]
public class DatasetsController(AppDbContext db, IWebHostEnvironment env) : ControllerBase
{
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    [HttpGet("project/{projectId:int}")]
    public async Task<IActionResult> List(int projectId)
    {
        var datasets = await db.Datasets
            .Where(d => d.ProjectId == projectId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
        return Ok(datasets.Select(ToDto));
    }

    [HttpPost("project/{projectId:int}/upload")]
    public async Task<IActionResult> Upload(int projectId, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { detail = "No file provided." });

        var allowed = new[] { ".csv", ".tsv", ".xlsx", ".xls", ".json" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowed.Contains(ext))
            return BadRequest(new { detail = $"Unsupported file type: {ext}" });

        // Save file
        var uploadsDir = Path.Combine(env.ContentRootPath, "uploads");
        Directory.CreateDirectory(uploadsDir);
        var savedName = $"{Guid.NewGuid():N}{ext}";
        var savedPath = Path.Combine(uploadsDir, savedName);
        await using (var fs = System.IO.File.Create(savedPath))
            await file.CopyToAsync(fs);

        // Parse
        ParsedFile parsed;
        try { parsed = FileParserService.Parse(savedPath); }
        catch (Exception ex) { return BadRequest(new { detail = ex.Message }); }

        // Schema
        var schema = parsed.Headers.ToDictionary(
            h => h,
            h =>
            {
                var sample = parsed.Rows.Take(5).Select(r => r.GetValueOrDefault(h)).FirstOrDefault(v => v != null);
                return sample is double or float or int or long ? "float64" : "object";
            });

        var chartDataJson = JsonSerializer.Serialize(new
        {
            headers = parsed.Headers,
            rows = parsed.Rows.Take(5000)
        });

        // Remove existing dataset for this project (one per project)
        var existing = await db.Datasets.Where(d => d.ProjectId == projectId).ToListAsync();
        db.Datasets.RemoveRange(existing);

        var ds = new DatasetEntity
        {
            ProjectId = projectId,
            FileName = file.FileName,
            FilePath = savedPath,
            RowCount = parsed.Rows.Count,
            ColCount = parsed.Headers.Count,
            SizeBytes = file.Length,
            SchemaJson = JsonSerializer.Serialize(schema),
            ChartDataJson = chartDataJson,
            CreatedAt = DateTime.UtcNow
        };
        db.Datasets.Add(ds);

        // Update project timestamp
        var proj = await db.Projects.FindAsync(projectId);
        if (proj != null) proj.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Ok(ToDto(ds));
    }

    [HttpGet("{id:int}/chart-data")]
    public async Task<IActionResult> ChartData(int id)
    {
        var ds = await db.Datasets.FindAsync(id);
        if (ds == null) return NotFound();
        if (string.IsNullOrEmpty(ds.ChartDataJson))
            return Ok(new { headers = Array.Empty<string>(), rows = Array.Empty<object>() });

        using var doc = JsonDocument.Parse(ds.ChartDataJson);
        return Ok(doc.RootElement);
    }

    [HttpPost("{id:int}/revive-session")]
    public async Task<IActionResult> ReviveSession(int id)
    {
        var ds = await db.Datasets.FindAsync(id);
        if (ds == null) return NotFound();
        // In .NET API, sessions are stateless — return stored IDs
        return Ok(new
        {
            ai_session_id = ds.AiSessionId,
            profile_context = ds.ProfileContext,
            revived = ds.AiSessionId != null
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var ds = await db.Datasets.FindAsync(id);
        if (ds == null) return NotFound();
        if (ds.FilePath != null && System.IO.File.Exists(ds.FilePath))
            System.IO.File.Delete(ds.FilePath);
        db.Datasets.Remove(ds);
        await db.SaveChangesAsync();
        return Ok(new { detail = "deleted" });
    }

    private static object ToDto(DatasetEntity d) => new
    {
        d.Id, d.ProjectId,
        file_name = d.FileName,
        row_count = d.RowCount,
        col_count = d.ColCount,
        size_bytes = d.SizeBytes,
        session_id = d.SessionId,
        ai_session_id = d.AiSessionId,
        profile_context = d.ProfileContext,
        schema_json = d.SchemaJson,
        created_at = d.CreatedAt
    };
}
