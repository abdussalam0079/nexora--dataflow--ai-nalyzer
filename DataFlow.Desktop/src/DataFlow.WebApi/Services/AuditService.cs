using DataFlow.WebApi.Data;

namespace DataFlow.WebApi.Services;

public sealed class AuditService(AppDbContext db, IHttpContextAccessor http)
{
    public async Task LogAsync(string action, string? entity = null,
        string? entityId = null, string? details = null, int? userId = null)
    {
        var ipAddress = http.HttpContext?.Connection.RemoteIpAddress?.ToString();
        var uid = userId ?? GetUserId(http.HttpContext);

        db.AuditLogs.Add(new AuditLogEntity
        {
            UserId    = uid,
            Action    = action,
            Entity    = entity,
            EntityId  = entityId,
            Details   = details,
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    private static int? GetUserId(HttpContext? ctx)
    {
        var claim = ctx?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                 ?? ctx?.User?.FindFirst("sub");
        return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
    }
}