using DataFlow.Core.Models;
using DataFlow.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DataFlow.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/projects")]
public class ProjectsController(AppDbContext db) : ControllerBase
{
    [HttpGet("")]
    public async Task<IActionResult> List()
    {
        var userId  = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var isAdmin = User.IsInRole("Admin");

        var query = db.Projects.AsQueryable();
        if (!isAdmin)
            query = query.Where(p => p.OwnerId == userId);

        var projects = await query
            .OrderByDescending(p => p.UpdatedAt)
            .Select(p => new
            {
                p.Id, p.Name, p.Description, p.Color, p.Icon,
                p.CreatedAt, p.UpdatedAt,
                DashboardCount = p.Dashboards.Count,
                DatasetCount   = p.Datasets.Count,
                ChatCount      = p.ChatSessions.Count,
            }).ToListAsync();
        return Ok(projects);
    }

    [HttpPost("")]
    public async Task<IActionResult> Create([FromBody] ProjectCreateRequest req)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var p = new ProjectEntity
        {
            OwnerId = userId, Name = req.Name, Description = req.Description,
            Color = req.Color ?? "#6366f1", Icon = req.Icon ?? "dashboard",
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        db.Projects.Add(p);
        await db.SaveChangesAsync();
        return Ok(ToDto(p));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var p = await db.Projects.FindAsync(id);
        if (p == null) return NotFound();
        return Ok(ToDto(p));
    }

    [HttpPatch("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ProjectCreateRequest req)
    {
        var p = await db.Projects.FindAsync(id);
        if (p == null) return NotFound();
        if (req.Name != null) p.Name = req.Name;
        if (req.Description != null) p.Description = req.Description;
        if (req.Color != null) p.Color = req.Color;
        if (req.Icon != null) p.Icon = req.Icon;
        p.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(ToDto(p));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var p = await db.Projects.FindAsync(id);
        if (p == null) return NotFound();
        db.Projects.Remove(p);
        await db.SaveChangesAsync();
        return Ok(new { detail = "deleted" });
    }

    [HttpGet("{id:int}/summary")]
    public async Task<IActionResult> Summary(int id)
    {
        var dashboards = await db.Dashboards
            .Where(d => d.ProjectId == id)
            .Select(d => new { d.Id, d.Name, d.IsPinned })
            .ToListAsync();
        var chats = await db.ChatSessions
            .Where(c => c.ProjectId == id)
            .OrderByDescending(c => c.UpdatedAt)
            .Take(10)
            .Select(c => new { c.Id, c.Title })
            .ToListAsync();
        return Ok(new { dashboards, chats });
    }

    private static object ToDto(ProjectEntity p) => new
    {
        p.Id, p.Name, p.Description, p.Color, p.Icon,
        p.CreatedAt, p.UpdatedAt,
        DashboardCount = 0, DatasetCount = 0, ChatCount = 0
    };
}
