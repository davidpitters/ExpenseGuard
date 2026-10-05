using ExpenseGuard.Domain.Organizations;
using Microsoft.EntityFrameworkCore;

namespace ExpenseGuard.Infrastructure.Identity;

// The only request-time unfiltered tenant lookup. It accepts an authenticated/credential-verified
// user ID and requires an explicit workspace or organization; it never returns tenant listings.
public sealed class IdentityDirectory(ExpenseGuardDbContext db)
{
    public Task<Membership?> FindForLoginAsync(Guid userId, string workspace, CancellationToken cancellationToken) =>
        (from membership in db.Memberships.IgnoreQueryFilters().AsNoTracking()
         join organization in db.Organizations.IgnoreQueryFilters().AsNoTracking()
             on membership.OrganizationId equals organization.Id
         where membership.UserId == userId && membership.IsActive && organization.Slug == workspace
         select membership).SingleOrDefaultAsync(cancellationToken);

    public Task<Membership?> FindForSessionAsync(Guid userId, Guid organizationId, CancellationToken cancellationToken) =>
        db.Memberships.IgnoreQueryFilters().AsNoTracking()
            .SingleOrDefaultAsync(m => m.UserId == userId && m.OrganizationId == organizationId && m.IsActive, cancellationToken);
}
