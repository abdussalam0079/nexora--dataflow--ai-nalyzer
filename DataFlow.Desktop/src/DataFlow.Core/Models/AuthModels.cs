namespace DataFlow.Core.Models;

public class LoginRequest
{
    public string Email    { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
{
    public string  Email       { get; set; } = string.Empty;
    public string  Password    { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
}

public class AuthResponse
{
    public string  AccessToken  { get; set; } = string.Empty;
    public string  RefreshToken { get; set; } = string.Empty;
    public int     ExpiresIn    { get; set; }
    public UserDto User         { get; set; } = new();
}

public class UserDto
{
    public int          Id          { get; set; }
    public string       Email       { get; set; } = string.Empty;
    public string       DisplayName { get; set; } = string.Empty;
    public string       Initials    { get; set; } = string.Empty;
    public string       Theme       { get; set; } = "dark";
    public List<string> Roles       { get; set; } = [];
    public DateTime     CreatedAt   { get; set; }
    public DateTime?    LastLoginAt { get; set; }

    public bool IsAdmin    => Roles.Contains("Admin");
    public bool IsAnalyst  => Roles.Contains("Analyst") || IsAdmin;
}

public class AdminStatsDto
{
    public int TotalUsers    { get; set; }
    public int TotalProjects { get; set; }
    public int TotalDatasets { get; set; }
    public int TotalChats    { get; set; }
    public int TotalMessages { get; set; }
}

public class AuditLogDto
{
    public int      Id        { get; set; }
    public string   Action    { get; set; } = string.Empty;
    public string?  Entity    { get; set; }
    public string?  EntityId  { get; set; }
    public string?  Details   { get; set; }
    public string?  IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
    public string?  UserEmail { get; set; }
    public string?  UserName  { get; set; }
}

public class AdminUserDto
{
    public int          Id           { get; set; }
    public string       Email        { get; set; } = string.Empty;
    public string       DisplayName  { get; set; } = string.Empty;
    public string?      AvatarInitials { get; set; }
    public DateTime     CreatedAt    { get; set; }
    public DateTime?    LastLoginAt  { get; set; }
    public List<string> Roles        { get; set; } = [];
    public int          ProjectCount { get; set; }
}