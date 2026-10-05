using ExpenseGuard.Application.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ExpenseGuard.Infrastructure.Identity;

public sealed class DesignTimeContextFactory : IDesignTimeDbContextFactory<ExpenseGuardDbContext>
{
    public ExpenseGuardDbContext CreateDbContext(string[] args) => new(
        new DbContextOptionsBuilder<ExpenseGuardDbContext>()
            .UseNpgsql("Host=localhost;Database=expenseguard;Username=expenseguard").Options, new EmptyTenant());

    private sealed class EmptyTenant : ITenantContext
    {
        public Guid? OrganizationId => null;
    }
}
