using Microsoft.EntityFrameworkCore;

namespace DataFlow.WebApi.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<UserEntity>        Users        => Set<UserEntity>();
    public DbSet<ProjectEntity>     Projects     => Set<ProjectEntity>();
    public DbSet<DatasetEntity>     Datasets     => Set<DatasetEntity>();
    public DbSet<DashboardEntity>   Dashboards   => Set<DashboardEntity>();
    public DbSet<ChatSessionEntity> ChatSessions => Set<ChatSessionEntity>();
    public DbSet<ChatMessageEntity> ChatMessages => Set<ChatMessageEntity>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<UserEntity>().HasData(new UserEntity
        {
            Id = 1, Name = "Default User", Email = "user@local.dev",
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
    }
}

public class UserEntity
{
    public int      Id        { get; set; }
    public string   Name      { get; set; } = "Default User";
    public string   Email     { get; set; } = "user@local.dev";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<ProjectEntity> Projects { get; set; } = [];
}

public class ProjectEntity
{
    public int      Id          { get; set; }
    public int      UserId      { get; set; } = 1;
    public string   Name        { get; set; } = string.Empty;
    public string?  Description { get; set; }
    public string   Color       { get; set; } = "#6366f1";
    public string   Icon        { get; set; } = "dashboard";
    public DateTime CreatedAt   { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt   { get; set; } = DateTime.UtcNow;
    public List<DatasetEntity>     Datasets     { get; set; } = [];
    public List<DashboardEntity>   Dashboards   { get; set; } = [];
    public List<ChatSessionEntity> ChatSessions { get; set; } = [];
}

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

public class ChatSessionEntity
{
    public int      Id        { get; set; }
    public int      ProjectId { get; set; }
    public int?     DatasetId { get; set; }
    public string?  Title     { get; set; }
    public string?  SessionId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ProjectEntity? Project { get; set; }
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
