using ExpenseGuard.Application.Identity;

namespace ExpenseGuard.Api.Identity;

public sealed class HttpTenantContext(IHttpContextAccessor accessor) : ITenantContext
{
    public const string OrganizationClaim = "org_id";
    public Guid? OrganizationId
    {
        get
        {
            var principal = accessor.HttpContext?.User;
            var claims = principal?.FindAll(OrganizationClaim).ToArray();
            return principal?.Identity?.IsAuthenticated == true && claims?.Length == 1
                && Guid.TryParse(claims[0].Value, out var value) && value != Guid.Empty ? value : null;
        }
    }
}
