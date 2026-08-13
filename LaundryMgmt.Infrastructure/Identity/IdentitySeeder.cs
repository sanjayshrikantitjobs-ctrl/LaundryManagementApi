using LaundryMgmt.Domain.Entities;
using LaundryMgmt.Domain.Enums;
using LaundryMgmt.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LaundryMgmt.Infrastructure.Identity;

/// <summary>
/// Dev-only bootstrap: ensures every <see cref="UserRole"/> exists as an Identity role,
/// then creates a default Admin login and a default Customer login if neither exists yet,
/// so there's an actual account to sign in with (README calls this out as a gap — no
/// RegisterCommand/seeder existed before this). Credentials come from configuration
/// ("SeedUsers" section) with fallback defaults; override them via user-secrets/appsettings
/// for anything beyond local development. Seeded users are pre-verified (unlike real
/// self-registration via RegisterCommand, which requires an OTP) since they're only
/// meant to give you something to log in with immediately.
/// </summary>
public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;

        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = provider.GetRequiredService<ApplicationDbContext>();
        var configuration = provider.GetRequiredService<IConfiguration>();
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(IdentitySeeder));

        // One-time data fix: UserRole.DeliveryBoy was renamed to DeliveryAgent. Rename
        // the existing AspNetRoles row in place (rather than creating a new orphaned
        // "DeliveryAgent" role) so any already-assigned users keep their role membership.
        var legacyDeliveryBoyRole = await roleManager.FindByNameAsync("DeliveryBoy");
        if (legacyDeliveryBoyRole is not null)
        {
            legacyDeliveryBoyRole.Name = nameof(UserRole.DeliveryAgent);
            await roleManager.UpdateAsync(legacyDeliveryBoyRole);
            logger.LogInformation("Renamed Identity role DeliveryBoy -> DeliveryAgent.");
        }

        foreach (var role in Enum.GetNames<UserRole>())
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
        }

        await EnsureUserAsync(
            userManager, logger,
            userName: configuration["SeedUsers:Admin:UserName"] ?? "admin",
            email: configuration["SeedUsers:Admin:Email"] ?? "admin@laundrymgmt.local",
            password: configuration["SeedUsers:Admin:Password"] ?? "Admin#12345",
            fullName: configuration["SeedUsers:Admin:FullName"] ?? "System Administrator",
            role: UserRole.Admin);

        var customerPhone = configuration["SeedUsers:Customer:PhoneNumber"] ?? "9999999999";
        var customerUser = await EnsureUserAsync(
            userManager, logger,
            userName: configuration["SeedUsers:Customer:UserName"] ?? "customer",
            email: configuration["SeedUsers:Customer:Email"] ?? "customer@laundrymgmt.local",
            password: configuration["SeedUsers:Customer:Password"] ?? "Customer#12345",
            fullName: configuration["SeedUsers:Customer:FullName"] ?? "Demo Customer",
            role: UserRole.Customer,
            phoneNumber: customerPhone);

        if (customerUser is not null && !await db.Customers.AnyAsync(c => c.IdentityUserId == customerUser.Id))
        {
            db.Customers.Add(new Customer
            {
                FullName = customerUser.FullName,
                PhoneNumber = customerPhone,
                Email = customerUser.Email,
                IdentityUserId = customerUser.Id
            });
            await db.SaveChangesAsync();
            logger.LogInformation("Linked a Customer catalog row to the seeded Customer login.");
        }
    }

    private static async Task<ApplicationUser?> EnsureUserAsync(
        UserManager<ApplicationUser> userManager, ILogger logger,
        string userName, string email, string password, string fullName, UserRole role, string? phoneNumber = null)
    {
        var existing = await userManager.FindByNameAsync(userName);
        if (existing is not null)
            return existing;

        var user = new ApplicationUser
        {
            UserName = userName,
            Email = email,
            EmailConfirmed = true,
            PhoneNumber = phoneNumber,
            PhoneNumberConfirmed = true,
            FullName = fullName,
            Role = role,
            IsActive = true
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            logger.LogWarning(
                "Seed user {UserName} was not created: {Errors}",
                userName, string.Join("; ", result.Errors.Select(e => e.Description)));
            return null;
        }

        await userManager.AddToRoleAsync(user, role.ToString());
        logger.LogInformation("Seeded {Role} login {UserName} ({Email})", role, userName, email);
        return user;
    }
}
