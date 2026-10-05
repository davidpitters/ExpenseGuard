using ExpenseGuard.Application.Identity;
using ExpenseGuard.Domain.Organizations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace ExpenseGuard.Infrastructure.Identity;

public sealed class DevelopmentSeed(
    DbContextOptions<ExpenseGuardDbContext> options,
    UserManager<ApplicationUser> users,
    IHostEnvironment environment)
{
    public static readonly Guid NorthstarId = Guid.Parse("f6226e88-e7d4-4810-a0b9-f772da5b9211");
    // Deliberately public, synthetic demo credential. IsDemo accounts cannot authenticate in Production.
    public const string Password = "Northstar-Demo-2026!";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!environment.IsDevelopment() && !environment.IsEnvironment("Test"))
        {
            throw new InvalidOperationException("Demo seeding is forbidden outside Development or Test.");
        }

        await using var db = new ExpenseGuardDbContext(options, new SeedTenant(NorthstarId));
        if (!await db.Organizations.AnyAsync(cancellationToken))
        {
            db.Organizations.Add(new Organization { Id = NorthstarId, Name = "Northstar Labs", Slug = "northstar-labs" });
            await db.SaveChangesAsync(cancellationToken);
        }

        var accounts = new[]
        {
            ("employee", "Alex Morgan", OrganizationRole.Employee, "39247f67-749a-43b5-9228-1a93b1c9e401"),
            ("finance", "Sam Chen", OrganizationRole.FinanceReviewer, "39247f67-749a-43b5-9228-1a93b1c9e402"),
            ("admin", "Jordan Lee", OrganizationRole.Administrator, "39247f67-749a-43b5-9228-1a93b1c9e403"),
            ("auditor", "Taylor Brooks", OrganizationRole.Auditor, "39247f67-749a-43b5-9228-1a93b1c9e404")
        };
        foreach (var (alias, name, role, id) in accounts)
        {
            var email = $"{alias}@northstar.example";
            var user = await users.FindByEmailAsync(email);
            if (user is null)
            {
                user = new ApplicationUser { Id = Guid.Parse(id), Email = email, UserName = email, DisplayName = name, EmailConfirmed = true, IsDemo = true };
                var result = await users.CreateAsync(user, Password);
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException("Synthetic account creation failed.");
                }
            }
            if (!user.IsDemo)
            {
                throw new InvalidOperationException("Demo seeding cannot modify an existing non-demo account.");
            }
            if (!await db.Memberships.AnyAsync(m => m.UserId == user.Id, cancellationToken))
            {
                db.Memberships.Add(new Membership { OrganizationId = NorthstarId, UserId = user.Id, Role = role });
                await db.SaveChangesAsync(cancellationToken);
            }
        }
    }

    private sealed record SeedTenant(Guid? OrganizationId) : ITenantContext;
}
