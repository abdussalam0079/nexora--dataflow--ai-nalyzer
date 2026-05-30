using System.Text;
using DataFlow.WebApi.Data;
using DataFlow.WebApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ── Controllers ───────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.PropertyNamingPolicy =
        System.Text.Json.JsonNamingPolicy.SnakeCaseLower);

// ── EF Core + SQLite ──────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlite($"Data Source={Path.Combine(
        builder.Environment.ContentRootPath, "dataflow.sqlite")}"));

// ── ASP.NET Core Identity ─────────────────────────────────────────
builder.Services.AddIdentity<AppUser, IdentityRole<int>>(opt =>
{
    opt.Password.RequiredLength         = 8;
    opt.Password.RequireNonAlphanumeric = false;
    opt.Password.RequireUppercase       = false;
    opt.Lockout.MaxFailedAccessAttempts = 5;
    opt.Lockout.DefaultLockoutTimeSpan  = TimeSpan.FromMinutes(5);
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// ── JWT Auth ──────────────────────────────────────────────────────
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret must be set in appsettings.json");

builder.Services.AddAuthentication(opt =>
{
    opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    opt.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(opt =>
{
    opt.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer   = true,
        ValidIssuer      = builder.Configuration["Jwt:Issuer"]   ?? "DataFlowApi",
        ValidateAudience = true,
        ValidAudience    = builder.Configuration["Jwt:Audience"] ?? "DataFlowApp",
        ValidateLifetime = true,
        ClockSkew        = TimeSpan.FromSeconds(30)
    };
});
builder.Services.AddAuthorization();

// ── App Services ──────────────────────────────────────────────────
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<GroqChatService>();
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<AuditService>();

// ── CORS ──────────────────────────────────────────────────────────
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// ── Build ─────────────────────────────────────────────────────────
var app = builder.Build();

// Auto-migrate + seed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();   // creates all tables including Identity tables
    await DbSeeder.SeedAsync(scope.ServiceProvider);
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/", () => new { status = "ok", version = "3.0.0" });

app.Run();
