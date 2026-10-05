using ExpenseGuard.Application.Identity;
using ExpenseGuard.Domain.Organizations;
using ExpenseGuard.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ExpenseGuard.Infrastructure;

public sealed class ExpenseGuardDbContext(
    DbContextOptions<ExpenseGuardDbContext> options, ITenantContext tenant)
    : IdentityUserContext<ApplicationUser, Guid>(options)
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Membership> Memberships => Set<Membership>();
    private Guid? CurrentOrganizationId => tenant.OrganizationId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasPostgresExtension("vector");
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(user => user.DisplayName).HasMaxLength(120);
            entity.HasIndex(user => user.NormalizedEmail).IsUnique();
        });
        modelBuilder.Entity<Organization>(entity =>
        {
            entity.HasKey(org => org.Id);
            entity.Property(org => org.Name).HasMaxLength(160);
            entity.Property(org => org.Slug).HasMaxLength(80);
            entity.HasIndex(org => org.Slug).IsUnique();
            entity.HasQueryFilter(org => CurrentOrganizationId != null && org.Id == CurrentOrganizationId);
        });
        modelBuilder.Entity<Membership>(entity =>
        {
            entity.HasKey(member => new { member.OrganizationId, member.UserId });
            entity.Property(member => member.Role).HasConversion<string>().HasMaxLength(32);
            entity.HasOne<Organization>().WithMany().HasForeignKey(member => member.OrganizationId);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(member => member.UserId);
            entity.HasQueryFilter(member => CurrentOrganizationId != null && member.OrganizationId == CurrentOrganizationId);
        });
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ValidateTenantWrites();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ValidateTenantWrites();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ValidateTenantWrites()
    {
        foreach (var entry in ChangeTracker.Entries().Where(entry =>
            entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            var organizationId = entry.Entity switch
            {
                Organization organization => organization.Id,
                ITenantRecord record => record.OrganizationId,
                _ => (Guid?)null
            };
            if (organizationId is not null && (tenant.OrganizationId is null || organizationId != tenant.OrganizationId))
            {
                throw new InvalidOperationException("Tenant-scoped writes require the authenticated organization.");
            }
            if (entry.Entity is ITenantRecord && entry.State is EntityState.Modified
                && entry.Property(nameof(ITenantRecord.OrganizationId)).IsModified)
            {
                throw new InvalidOperationException("Tenant ownership cannot be changed.");
            }
        }
    }
}
