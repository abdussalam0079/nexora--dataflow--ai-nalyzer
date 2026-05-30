using DataFlow.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DataFlow.WebApi.Controllers;

[ApiController]
[Route("api/v1/audit")]
[Authorize(Roles = "Admin,Analyst")]
public class AuditController(AppDbContext db) : ControllerBase
{
    [HttpGet("")]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        [FromQuery] string? action = null)
    {
        var query = db.AuditLogs
            .Include(a => a.User)
            .AsQueryable();

        if (!string.IsNullOrEmpty(action))
            query = query.Where(a => a.Action.Contains(action));

        var total = await query.CountAsync();
        var logs  = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                a.Id,
                a.Action,
                a.Entity,
                a.EntityId,
                a.Details,
                a.IpAddress,
                a.CreatedAt,
                UserEmail = a.User != null ? a.User.Email : "system",
                UserName  = a.User != null ? a.User.DisplayName : "System"
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, logs });
    }
}