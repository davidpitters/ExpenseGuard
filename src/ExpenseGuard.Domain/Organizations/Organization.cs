namespace ExpenseGuard.Domain.Organizations;

public interface ITenantRecord
{
    Guid OrganizationId { get; }
}

public sealed class Organization
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
}

public enum OrganizationRole
{
    Employee,
    FinanceReviewer,
    Administrator,
    Auditor
}

public sealed class Membership : ITenantRecord
{
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public OrganizationRole Role { get; set; }
    public bool IsActive { get; set; } = true;
}
