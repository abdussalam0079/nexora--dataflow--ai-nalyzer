using DataFlow.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DataFlow.WebApi.Controllers;

[ApiController]
[Route("api/v1/admin")]
[Authorize(Roles = "Admin")]
public class AdminController(
    UserManager<AppUser> userManager,
    AppDbContext db) : ControllerBase
{
    // ── Users ─────────────────────────────────────────────────────
    [HttpGet("users")]
    public async Task<IActionResult> ListUsers()
    {
        var users = await userManager.Users
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        var result = new List<object>();
        foreach (var u in users)
        {
            var roles = await userManager.GetRolesAsync(u);
            result.Add(new
            {
                u.Id, u.Email, u.DisplayName, u.AvatarInitials,
                u.CreatedAt, u.LastLoginAt,
                Roles = roles,
                ProjectCount = await db.Projects.CountAsync(p => p.OwnerId == u.Id)
            });
        }
        return Ok(result);
    }

    [HttpPatch("users/{id:int}/role")]
    public async Task<IActionResult> SetRole(int id, [FromBody] SetRoleRequest req)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound();

        var currentRoles = await userManager.GetRolesAsync(user);
        await userManager.RemoveFromRolesAsync(user, currentRoles);
        await userManager.AddToRoleAsync(user, req.Role);
        return Ok(new { message = $"Role set to {req.Role}" });
    }

    [HttpDelete("users/{id:int}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound();
        await userManager.DeleteAsync(user);
        return Ok(new { message = "User deleted." });
    }

    // ── Stats ─────────────────────────────────────────────────────
    [HttpGet("stats")]
    public async Task<IActionResult> Stats()
    {
        return Ok(new
        {
            TotalUsers    = await userManager.Users.CountAsync(),
            TotalProjects = await db.Projects.CountAsync(),
            TotalDatasets = await db.Datasets.CountAsync(),
            TotalChats    = await db.ChatSessions.CountAsync(),
            TotalMessages = await db.ChatMessages.CountAsync(),
        });
    }

    // ── Audit ─────────────────────────────────────────────────────
    [HttpGet("audit")]
    public async Task<IActionResult> AuditFeed([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var logs = await db.AuditLogs
            .Include(a => a.User)
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                a.Id, a.Action, a.Entity, a.EntityId, a.Details,
                a.IpAddress, a.CreatedAt,
                UserEmail = a.User != null ? a.User.Email : "system"
            })
            .ToListAsync();

        return Ok(logs);
    }
}

public record SetRoleRequest(string Role);