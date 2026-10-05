using System.Net;
using System.Security.Claims;
using System.Text.Json.Serialization;
using ExpenseGuard.Application.Identity;
using ExpenseGuard.Domain.Organizations;
using ExpenseGuard.Infrastructure;
using ExpenseGuard.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ExpenseGuard.Api.Identity;

public static class IdentitySetup
{
    public static void AddExpenseGuardIdentity(this WebApplicationBuilder builder)
    {
        var development = builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Test");
        if (!development && (builder.Configuration.GetValue<bool>("Demo:Seed")
            || builder.Configuration.GetValue<bool>("Database:Initialize")))
        {
            throw new InvalidOperationException("Development initialization is forbidden in this environment.");
        }
        builder.Services.AddHttpContextAccessor();
        builder.Services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow);
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
            options.KnownProxies.Add(IPAddress.Loopback);
            options.KnownProxies.Add(IPAddress.IPv6Loopback);
            options.ForwardLimit = 1;
        });
        builder.Services.AddScoped<ITenantContext, HttpTenantContext>();
        builder.Services.AddDbContext<ExpenseGuardDbContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));
        builder.Services.AddScoped<IdentityDirectory>();
        builder.Services.AddScoped<DevelopmentSeed>();
        builder.Services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequiredLength = 12;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        }).AddEntityFrameworkStores<ExpenseGuardDbContext>().AddSignInManager();

        builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme).AddIdentityCookies();
        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = "__Host-ExpenseGuard.Session";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.Path = "/";
            options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
            options.SlidingExpiration = false;
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
            options.Events.OnValidatePrincipal = async context =>
            {
                var idClaims = context.Principal?.FindAll(ClaimTypes.NameIdentifier).ToArray();
                var orgClaims = context.Principal?.FindAll(HttpTenantContext.OrganizationClaim).ToArray();
                var roleClaims = context.Principal?.FindAll(ClaimTypes.Role).ToArray();
                var valid = idClaims?.Length == 1 && orgClaims?.Length == 1 && roleClaims?.Length == 1
                    && Guid.TryParse(idClaims[0].Value, out _) && Guid.TryParse(orgClaims[0].Value, out _);
                if (valid)
                {
                    var services = context.HttpContext.RequestServices;
                    var environment = services.GetRequiredService<IHostEnvironment>();
                    var manager = services.GetRequiredService<UserManager<ApplicationUser>>();
                    var user = await manager.FindByIdAsync(idClaims![0].Value);
                    var membership = await services.GetRequiredService<IdentityDirectory>().FindForSessionAsync(
                        Guid.Parse(idClaims[0].Value), Guid.Parse(orgClaims![0].Value), context.HttpContext.RequestAborted);
                    valid = user is not null && membership is not null
                        && (!user.IsDemo || environment.IsDevelopment() || environment.IsEnvironment("Test"))
                        && !await manager.IsLockedOutAsync(user)
                        && await manager.GetSecurityStampAsync(user) == context.Principal!.FindFirstValue("AspNet.Identity.SecurityStamp")
                        && membership.Role.ToString() == roleClaims![0].Value;
                }
                if (!valid)
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
                }
            };
        });
        builder.Services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
            options.Cookie.Name = "__Host-ExpenseGuard.Csrf";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.Path = "/";
        });
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("OrganizationMember", policy => policy.RequireAuthenticatedUser()
                .RequireClaim(HttpTenantContext.OrganizationClaim).RequireRole(Enum.GetNames<OrganizationRole>()));
            options.AddPolicy("MemberDirectory", policy => policy.RequireAuthenticatedUser()
                .RequireClaim(HttpTenantContext.OrganizationClaim)
                .RequireRole(nameof(OrganizationRole.Administrator), nameof(OrganizationRole.Auditor)));
            options.AddPolicy("FinanceDecision", policy => policy.RequireAuthenticatedUser()
                .RequireClaim(HttpTenantContext.OrganizationClaim).RequireRole(nameof(OrganizationRole.FinanceReviewer)));
            options.AddPolicy("OrganizationAdministration", policy => policy.RequireAuthenticatedUser()
                .RequireClaim(HttpTenantContext.OrganizationClaim).RequireRole(nameof(OrganizationRole.Administrator)));
        });
    }
}
