using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DataFlow.WebApi.Data;

// ─── Identity user ────────────────────────────────────────────────
public class AppUser : IdentityUser<int>
{
    public string   DisplayName   { get; set; } = string.Empty;
    public string?  AvatarInitials{ get; set; }
    public string   Theme         { get; set; } = "dark";
    public string?  EncryptedApiKey { get; set; }   // Groq key per-user
    public DateTime CreatedAt     { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt  { get; set; }

    public List<ProjectEntity>  Projects  { get; set; } = [];
    public List<AuditLogEntity> AuditLogs { get; set; } = [];
    public List<RefreshTokenEntity> RefreshTokens { get; set; } = [];
}

// ─── Refresh tokens ───────────────────────────────────────────────
public class RefreshTokenEntity
{
    public int      Id        { get; set; }
    public int      UserId    { get; set; }
    public string   Token     { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool     Revoked   { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public AppUser? User      { get; set; }
}

// ─── Audit log ────────────────────────────────────────────────────
public class AuditLogEntity
{
    public int      Id         { get; set; }
    public int?     UserId     { get; set; }
    public string   Action     { get; set; } = string.Empty;
    public string?  Entity     { get; set; }
    public string?  EntityId   { get; set; }
    public string?  Details    { get; set; }
    public string?  IpAddress  { get; set; }
    public DateTime CreatedAt  { get; set; } = DateTime.UtcNow;
    public AppUser? User       { get; set; }
}

// ─── Project ──────────────────────────────────────────────────────
public class ProjectEntity
{
    public int      Id          { get; set; }
    public int      OwnerId     { get; set; }           // FK to AppUser
    public string   Name        { get; set; } = string.Empty;
    public string?  Description { get; set; }
    public string   Color       { get; set; } = "#6366f1";
    public string   Icon        { get; set; } = "dashboard";
    public DateTime CreatedAt   { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt   { get; set; } = DateTime.UtcNow;

    public AppUser? Owner        { get; set; }
    public List<DatasetEntity>     Datasets     { get; set; } = [];
    public List<DashboardEntity>   Dashboards   { get; set; } = [];
    public List<ChatSessionEntity> ChatSessions { get; set; } = [];
}

// ─── Dataset ──────────────────────────────────────────────────────
public class DatasetEntity
{
    public int      Id             { get; set; }
    public int      ProjectId      { get; set; }
    public string   FileName       { get; set; } = string.Empty;
    public string?  FilePath       { get; set; }
    public string?  SessionId      { get; set; }
    public string?  AiSessionId    { get; set; }
    public string?  SchemaJson     { get; set; }
    public string?  ChartDataJson  { get; set; }
    public string?  ProfileContext { get; set; }
    public int      RowCount       { get; set; }
    public int      ColCount       { get; set; }
    public long     SizeBytes      { get; set; }
    public DateTime CreatedAt      { get; set; } = DateTime.UtcNow;
    public ProjectEntity? Project  { get; set; }
}

// ─── Dashboard ────────────────────────────────────────────────────
public class DashboardEntity
{
    public int      Id          { get; set; }
    public int      ProjectId   { get; set; }
    public int?     DatasetId   { get; set; }
    public string   Name        { get; set; } = string.Empty;
    public string?  Description { get; set; }
    public string   Scheme      { get; set; } = "Metric Flow";
    public string?  LayoutJson  { get; set; }
    public bool     IsPinned    { get; set; }
    public DateTime CreatedAt   { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt   { get; set; } = DateTime.UtcNow;
    public ProjectEntity? Project { get; set; }
}

// ─── Chat ─────────────────────────────────────────────────────────
public class ChatSessionEntity
{
    public int      Id        { get; set; }
    public int      ProjectId { get; set; }
    public int?     DatasetId { get; set; }
    public string?  Title     { get; set; }
    public string?  SessionId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ProjectEntity? Project  { get; set; }
    public List<ChatMessageEntity> Messages { get; set; } = [];
}

public class ChatMessageEntity
{
    public int      Id        { get; set; }
    public int      SessionId { get; set; }
    public string   Role      { get; set; } = "user";
    public string   Content   { get; set; } = string.Empty;
    public bool     HasChart  { get; set; }
    public string?  ChartJson { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ChatSessionEntity? Session { get; set; }
}

// ─── DbContext ────────────────────────────────────────────────────
public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<int>, int>(options)
{
    public DbSet<ProjectEntity>     Projects      => Set<ProjectEntity>();
    public DbSet<DatasetEntity>     Datasets      => Set<DatasetEntity>();
    public DbSet<DashboardEntity>   Dashboards    => Set<DashboardEntity>();
    public DbSet<ChatSessionEntity> ChatSessions  => Set<ChatSessionEntity>();
    public DbSet<ChatMessageEntity> ChatMessages  => Set<ChatMessageEntity>();
    public DbSet<AuditLogEntity>    AuditLogs     => Set<AuditLogEntity>();
    public DbSet<RefreshTokenEntity> RefreshTokens => Set<RefreshTokenEntity>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);   // <-- required for Identity tables

        b.Entity<ProjectEntity>()
            .HasOne(p => p.Owner)
            .WithMany(u => u.Projects)
            .HasForeignKey(p => p.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<AuditLogEntity>()
            .HasOne(a => a.User)
            .WithMany(u => u.AuditLogs)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        b.Entity<RefreshTokenEntity>()
            .HasOne(r => r.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}