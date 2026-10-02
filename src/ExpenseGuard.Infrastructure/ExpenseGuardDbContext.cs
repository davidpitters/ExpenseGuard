using Microsoft.EntityFrameworkCore;

namespace ExpenseGuard.Infrastructure;

// Entity mappings and deterministic migrations are introduced with milestone 1.
public sealed class ExpenseGuardDbContext(DbContextOptions<ExpenseGuardDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasPostgresExtension("vector");
    }
}
