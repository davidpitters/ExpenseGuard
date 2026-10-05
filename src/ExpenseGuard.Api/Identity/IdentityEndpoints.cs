using System.Security.Claims;
using ExpenseGuard.Application.Identity;
using ExpenseGuard.Infrastructure;
using ExpenseGuard.Infrastructure.Identity;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ExpenseGuard.Api.Identity;

public static class IdentityEndpoints
{
    public static void MapIdentityEndpoints(this WebApplication app)
    {
        app.MapGet("/api/auth/csrf", Results<Ok<CsrfResponse>, ProblemHttpResult> (HttpContext context, IAntiforgery antiforgery) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            if (!context.Request.IsHttps)
            {
                return TypedResults.Problem(statusCode: 426, title: "Sign-in requires HTTPS.");
            }
            return TypedResults.Ok(new CsrfResponse(antiforgery.GetAndStoreTokens(context).RequestToken!));
        }).WithName("GetCsrfToken");

        app.MapPost("/api/auth/login", LoginAsync)
            .WithName("Login").AddEndpointFilter<AntiforgeryFilter>().AddEndpointFilter<DatabaseConfiguredFilter>();

        app.MapPost("/api/auth/logout", async (SignInManager<ApplicationUser> manager) =>
        {
            await manager.SignOutAsync();
            return TypedResults.NoContent();
        }).RequireAuthorization("OrganizationMember").AddEndpointFilter<AntiforgeryFilter>().WithName("Logout");

        app.MapGet("/api/auth/session", (HttpContext context) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            return new SessionResponse(Guid.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier)!),
                context.User.FindFirstValue("display_name")!,
                Guid.Parse(context.User.FindFirstValue(HttpTenantContext.OrganizationClaim)!),
                context.User.FindFirstValue(ClaimTypes.Role)!);
        }).RequireAuthorization("OrganizationMember").WithName("GetSession");

        app.MapGet("/api/organizations/{organizationId:guid}", async Task<Results<Ok<OrganizationResponse>, NotFound>> (
            Guid organizationId, ExpenseGuardDbContext db, CancellationToken cancellationToken) =>
        {
            var organization = await db.Organizations.AsNoTracking()
                .Where(org => org.Id == organizationId)
                .Select(org => new OrganizationResponse(org.Id, org.Name, org.Slug))
                .SingleOrDefaultAsync(cancellationToken);
            return organization is null ? TypedResults.NotFound() : TypedResults.Ok(organization);
        }).RequireAuthorization("OrganizationMember").WithName("GetOrganization");

        app.MapGet("/api/organizations/{organizationId:guid}/members", async Task<Results<Ok<MembersResponse>, NotFound, BadRequest>> (
            Guid organizationId, int? page, ExpenseGuardDbContext db, CancellationToken cancellationToken) =>
        {
            var currentPage = page ?? 1;
            if (currentPage is < 1 or > 10000) { return TypedResults.BadRequest(); }
            if (!await db.Organizations.AnyAsync(org => org.Id == organizationId, cancellationToken))
            {
                return TypedResults.NotFound();
            }
            const int pageSize = 25;
            var members = await (from member in db.Memberships.AsNoTracking()
                                 join user in db.Users.AsNoTracking() on member.UserId equals user.Id
                                 where member.OrganizationId == organizationId && member.IsActive
                                 orderby user.DisplayName, member.UserId
                                 select new MemberResponse(user.Id, user.DisplayName, member.Role.ToString()))
                .Skip((currentPage - 1) * pageSize).Take(pageSize + 1).ToListAsync(cancellationToken);
            return TypedResults.Ok(new MembersResponse(members.Take(pageSize).ToArray(), currentPage, pageSize, members.Count > pageSize));
        }).RequireAuthorization("MemberDirectory").WithName("GetMembers");
    }

    private static async Task<Results<NoContent, UnauthorizedHttpResult, ValidationProblem>> LoginAsync(
        LoginRequest request, SignInManager<ApplicationUser> manager, UserManager<ApplicationUser> users,
        IdentityDirectory directory, IHostEnvironment environment, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || request.Email.Length > 254
            || string.IsNullOrEmpty(request.Password) || request.Password.Length > 256
            || string.IsNullOrWhiteSpace(request.Workspace) || request.Workspace.Length > 80)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["credentials"] = ["A valid email, password and workspace are required."]
            });
        }
        var user = await users.FindByEmailAsync(request.Email.Trim());
        var development = environment.IsDevelopment() || environment.IsEnvironment("Test");
        if (user is null || (user.IsDemo && !development)
            || !(await manager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true)).Succeeded)
        {
            return TypedResults.Unauthorized();
        }
        var membership = await directory.FindForLoginAsync(user.Id, request.Workspace.Trim().ToLowerInvariant(), cancellationToken);
        if (membership is null) { return TypedResults.Unauthorized(); }

        await manager.SignInWithClaimsAsync(user, isPersistent: false, [
            new Claim(HttpTenantContext.OrganizationClaim, membership.OrganizationId.ToString()),
            new Claim(ClaimTypes.Role, membership.Role.ToString()),
            new Claim("display_name", user.DisplayName)
        ]);
        return TypedResults.NoContent();
    }
}

public sealed class AntiforgeryFilter(IAntiforgery antiforgery) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (!context.HttpContext.Request.IsHttps)
        {
            return Results.Problem(statusCode: 426, title: "Sign-in requires HTTPS.");
        }
        try { await antiforgery.ValidateRequestAsync(context.HttpContext); }
        catch (AntiforgeryValidationException)
        {
            return Results.Problem(statusCode: 400, title: "Request verification failed.", extensions: new Dictionary<string, object?> { ["code"] = "csrf_invalid" });
        }
        return await next(context);
    }
}

public sealed class DatabaseConfiguredFilter(IConfiguration configuration) : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next) =>
        string.IsNullOrWhiteSpace(configuration.GetConnectionString("Postgres"))
            ? ValueTask.FromResult<object?>(Results.Problem(statusCode: 503, title: "Identity storage is not configured."))
            : next(context);
}
