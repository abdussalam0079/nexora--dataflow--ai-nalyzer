using Microsoft.AspNetCore.Identity;

namespace DataFlow.WebApi.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<int>>>();
        var userManager = services.GetRequiredService<UserManager<AppUser>>();
        var config      = services.GetRequiredService<IConfiguration>();

        // Seed roles
        string[] roles = ["Admin", "Analyst", "User"];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<int>(role));
        }

        // Seed default admin
        var adminEmail    = config["Seed:AdminEmail"]    ?? "admin@nexora.local";
        var adminPassword = config["Seed:AdminPassword"] ?? "Admin@1234!";

        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var admin = new AppUser
            {
                UserName      = adminEmail,
                Email         = adminEmail,
                DisplayName   = "System Admin",
                AvatarInitials= "SA",
                CreatedAt     = DateTime.UtcNow,
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(admin, adminPassword);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, "Admin");
        }
    }
}