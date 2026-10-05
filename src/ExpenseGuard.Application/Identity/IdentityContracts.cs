namespace ExpenseGuard.Application.Identity;

public interface ITenantContext
{
    Guid? OrganizationId { get; }
}

public sealed record LoginRequest(string Email, string Password, string Workspace);
public sealed record CsrfResponse(string RequestToken);
public sealed record SessionResponse(Guid UserId, string DisplayName, Guid OrganizationId, string Role);
public sealed record OrganizationResponse(Guid Id, string Name, string Slug);
public sealed record MemberResponse(Guid UserId, string DisplayName, string Role);
public sealed record MembersResponse(IReadOnlyList<MemberResponse> Items, int Page, int PageSize, bool HasMore);
