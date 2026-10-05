using Microsoft.AspNetCore.Identity;

namespace ExpenseGuard.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public required string DisplayName { get; set; }
    public bool IsDemo { get; set; }
}
