using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace schedule_ders.Data;

public static class IdentitySeeder
{
    private static readonly string[] Roles = ["Admin", "Professor", "Student"];
    private static readonly (string NewEmail, string[] LegacyEmails, string Role)[] DemoUsers =
    [
        ("admin@email.com", ["admin@scheduleders.app", "admin@scheduleders.local"], "Admin"),
        ("professor@email.com", ["professor@scheduleders.app", "professor@scheduleders.local"], "Professor"),
        ("student@email.com", ["student@scheduleders.app", "student@scheduleders.local"], "Student")
    ];

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

        foreach (var demoUser in DemoUsers)
        {
            await EnsureUserInRoleAsync(userManager, demoUser.NewEmail, demoUser.LegacyEmails, demoUser.Role, demoPassword);
        }
    }

    private static async Task EnsureUserInRoleAsync(
        UserManager<IdentityUser> userManager,
        string email,
        string[] legacyEmails,
        string role,
        string password)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            foreach (var legacyEmail in legacyEmails)
            {
                user = await userManager.FindByEmailAsync(legacyEmail);
                if (user is not null)
                {
                    break;
                }
            }
        }

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
        else if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase)
                 || !string.Equals(user.UserName, email, StringComparison.OrdinalIgnoreCase))
        {
            user.Email = email;
            user.UserName = email;
            user.NormalizedEmail = userManager.NormalizeEmail(email);
            user.NormalizedUserName = userManager.NormalizeName(email);

            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                throw new InvalidOperationException($"Failed to update user '{email}': {string.Join(", ", updateResult.Errors.Select(e => e.Description))}");
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
