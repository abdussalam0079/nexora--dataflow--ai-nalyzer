using DataFlow.WebApi.Data;
using DataFlow.WebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DataFlow.WebApi.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(
    UserManager<AppUser>  userManager,
    SignInManager<AppUser> signInManager,
    JwtService            jwt,
    AppDbContext          db,
    AuditService          audit) : ControllerBase
{
    // ── REGISTER ──────────────────────────────────────────────────
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { error = "Email and password are required." });

        var user = new AppUser
        {
            UserName      = req.Email,
            Email         = req.Email,
            DisplayName   = req.DisplayName ?? req.Email.Split('@')[0],
            AvatarInitials = BuildInitials(req.DisplayName ?? req.Email),
            CreatedAt     = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, req.Password);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        // Assign default role (Ensure "User" role exists in your DB)
        await userManager.AddToRoleAsync(user, "User");
        await audit.LogAsync("USER_REGISTER", "AppUser", user.Id.ToString(), user.Email);

        return Ok(new { message = "Account created. Please log in." });
    }

    // ── LOGIN ─────────────────────────────────────────────────────
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var user = await userManager.FindByEmailAsync(req.Email);
        if (user == null)
            return Unauthorized(new { error = "Invalid credentials." });

        var signIn = await signInManager.CheckPasswordSignInAsync(user, req.Password, lockoutOnFailure: true);
        if (!signIn.Succeeded)
        {
            await audit.LogAsync("LOGIN_FAILED", "AppUser", user.Id.ToString(), req.Email);
            return Unauthorized(new { error = "Invalid credentials." });
        }

        user.LastLoginAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);

        var roles = await userManager.GetRolesAsync(user);
        var accessToken  = jwt.GenerateAccessToken(user, roles);
        var (refreshToken, refreshExpiry) = jwt.GenerateRefreshToken();

        db.RefreshTokens.Add(new RefreshTokenEntity
        {
            UserId    = user.Id,
            Token     = refreshToken,
            ExpiresAt = refreshExpiry
        });
        await db.SaveChangesAsync();

        await audit.LogAsync("LOGIN_SUCCESS", "AppUser", user.Id.ToString(), user.Email, user.Id);

        return Ok(new AuthResponse
        {
            AccessToken  = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn    = 3600,
            User         = MapUser(user, roles)
        });
    }

    // ── REFRESH ───────────────────────────────────────────────────
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest req)
    {
        var stored = await db.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Token == req.RefreshToken && !r.Revoked);

        if (stored == null || stored.ExpiresAt < DateTime.UtcNow)
            return Unauthorized(new { error = "Invalid or expired refresh token." });

        // Rotate
        stored.Revoked = true;
        var user  = stored.User!;
        var roles = await userManager.GetRolesAsync(user);
        var accessToken  = jwt.GenerateAccessToken(user, roles);
        var (newRefresh, newExpiry) = jwt.GenerateRefreshToken();

        db.RefreshTokens.Add(new RefreshTokenEntity
        {
            UserId    = user.Id,
            Token     = newRefresh,
            ExpiresAt = newExpiry
        });
        await db.SaveChangesAsync();

        return Ok(new AuthResponse
        {
            AccessToken  = accessToken,
            RefreshToken = newRefresh,
            ExpiresIn    = 3600,
            User         = MapUser(user, roles)
        });
    }

    // ── LOGOUT ────────────────────────────────────────────────────
    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest req)
    {
        var token = await db.RefreshTokens
            .FirstOrDefaultAsync(r => r.Token == req.RefreshToken);
        if (token != null) token.Revoked = true;
        await db.SaveChangesAsync();
        return Ok(new { message = "Logged out." });
    }

    // ── ME ────────────────────────────────────────────────────────
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();
        var roles = await userManager.GetRolesAsync(user);
        return Ok(MapUser(user, roles));
    }

    // ── HELPERS ───────────────────────────────────────────────────
    private static UserDto MapUser(AppUser user, IList<string> roles) => new()
    {
        Id           = user.Id,
        Email        = user.Email ?? "",
        DisplayName  = user.DisplayName,
        Initials     = user.AvatarInitials ?? BuildInitials(user.DisplayName),
        Theme        = user.Theme,
        Roles        = [.. roles],
        CreatedAt    = user.CreatedAt,
        LastLoginAt  = user.LastLoginAt
    };

    private static string BuildInitials(string name)
    {
        var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2
            ? $"{parts[0][0]}{parts[^1][0]}".ToUpper()
            : name.Length >= 2 ? name[..2].ToUpper() : name.ToUpper();
    }
}

// ─── Request / Response DTOs ──────────────────────────────────────
public record RegisterRequest(string Email, string Password, string? DisplayName);
public record LoginRequest(string Email, string Password);
public record RefreshRequest(string RefreshToken);
public record LogoutRequest(string RefreshToken);

public class AuthResponse
{
    public string   AccessToken  { get; set; } = string.Empty;
    public string   RefreshToken { get; set; } = string.Empty;
    public int      ExpiresIn    { get; set; }
    public UserDto  User         { get; set; } = new();
}

public class UserDto
{
    public int       Id          { get; set; }
    public string    Email       { get; set; } = string.Empty;
    public string    DisplayName { get; set; } = string.Empty;
    public string    Initials    { get; set; } = string.Empty;
    public string    Theme       { get; set; } = "dark";
    public List<string> Roles    { get; set; } = [];
    public DateTime  CreatedAt   { get; set; }
    public DateTime? LastLoginAt { get; set; }
}