using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace schedule_ders.Data;

public static class IdentitySeeder
{
    private static readonly string[] Roles = ["Admin", "Professor", "Student"];

    public static async Task SeedAsync(IServiceProvider services, bool seedDemoUsers)
    {
        using var scope = services.CreateScope();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var roleResult = await roleManager.CreateAsync(new IdentityRole(role));
                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException($"Failed to create role '{role}': {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                }
            }
        }

        if (!seedDemoUsers)
        {
            return;
        }

        var demoPassword = configuration["Seed:DemoUserPassword"];
        if (string.IsNullOrWhiteSpace(demoPassword))
        {
            return;
        }

        await EnsureUserInRoleAsync(userManager, "admin@scheduleders.app", "Admin", demoPassword);
        await EnsureUserInRoleAsync(userManager, "professor@scheduleders.app", "Professor", demoPassword);
        await EnsureUserInRoleAsync(userManager, "student@scheduleders.app", "Student", demoPassword);
    }

    private static async Task EnsureUserInRoleAsync(UserManager<IdentityUser> userManager, string email, string role, string password)
    {
        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            user = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException($"Failed to create user '{email}': {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
            }
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            var addRoleResult = await userManager.AddToRoleAsync(user, role);
            if (!addRoleResult.Succeeded)
            {
                throw new InvalidOperationException($"Failed to add user '{email}' to role '{role}': {string.Join(", ", addRoleResult.Errors.Select(e => e.Description))}");
            }
        }
    }
}
