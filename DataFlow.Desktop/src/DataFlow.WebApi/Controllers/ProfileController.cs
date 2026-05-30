using DataFlow.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DataFlow.WebApi.Controllers;

[ApiController]
[Route("api/v1/profile")]
[Authorize]
public class ProfileController(UserManager<AppUser> userManager) : ControllerBase
{
    [HttpGet("")]
    public async Task<IActionResult> Get()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();
        var roles = await userManager.GetRolesAsync(user);
        return Ok(new
        {
            user.Id, user.Email, user.DisplayName,
            user.AvatarInitials, user.Theme,
            Roles = roles,
            user.CreatedAt, user.LastLoginAt
        });
    }

    [HttpPatch("")]
    public async Task<IActionResult> Update([FromBody] UpdateProfileRequest req)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        if (!string.IsNullOrWhiteSpace(req.DisplayName))
        {
            user.DisplayName   = req.DisplayName;
            user.AvatarInitials = BuildInitials(req.DisplayName);
        }
        if (!string.IsNullOrWhiteSpace(req.Theme))
            user.Theme = req.Theme;

        await userManager.UpdateAsync(user);
        return Ok(new { message = "Profile updated." });
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var result = await userManager.ChangePasswordAsync(user, req.CurrentPassword, req.NewPassword);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        return Ok(new { message = "Password changed successfully." });
    }

    [HttpPost("api-key")]
    public async Task<IActionResult> SaveApiKey([FromBody] SaveApiKeyRequest req)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        // Basic obfuscation — in production use AES encryption with a server key
        user.EncryptedApiKey = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes(req.ApiKey));
        await userManager.UpdateAsync(user);
        return Ok(new { message = "API key saved." });
    }

    private static string BuildInitials(string name)
    {
        var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2
            ? $"{parts[0][0]}{parts[^1][0]}".ToUpper()
            : name.Length >= 2 ? name[..2].ToUpper() : name.ToUpper();
    }
}

public record UpdateProfileRequest(string? DisplayName, string? Theme);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public record SaveApiKeyRequest(string ApiKey);
