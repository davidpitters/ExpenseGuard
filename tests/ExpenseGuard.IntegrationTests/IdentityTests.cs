using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using ExpenseGuard.Application.Identity;
using ExpenseGuard.Domain.Organizations;
using ExpenseGuard.Infrastructure;
using ExpenseGuard.Infrastructure.Identity;
using ExpenseGuard.ServiceDefaults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ExpenseGuard.IntegrationTests;

public sealed class IdentityTests
{
    [Theory]
    [InlineData("employee", "Employee", HttpStatusCode.Forbidden, false)]
    [InlineData("finance", "FinanceReviewer", HttpStatusCode.Forbidden, true)]
    [InlineData("admin", "Administrator", HttpStatusCode.OK, false)]
    [InlineData("auditor", "Auditor", HttpStatusCode.OK, false)]
    public async Task Membership_roles_control_access(string account, string role, HttpStatusCode directoryStatus, bool mayDecide)
    {
        await using var factory = await IdentityFactory.CreateAsync();
        using var client = factory.HttpsClient();
        var login = await LoginAsync(client, account);
        Assert.Equal(HttpStatusCode.NoContent, login.StatusCode);
        var session = await client.GetFromJsonAsync<SessionResponse>("/api/auth/session");
        Assert.NotNull(session);
        Assert.Equal(role, session.Role);
        Assert.Equal(DevelopmentSeed.NorthstarId, session.OrganizationId);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/organizations/{session.OrganizationId}")).StatusCode);
        var members = await client.GetAsync($"/api/organizations/{session.OrganizationId}/members");
        Assert.Equal(directoryStatus, members.StatusCode);
        if (members.IsSuccessStatusCode)
        {
            var result = await members.Content.ReadFromJsonAsync<MembersResponse>();
            Assert.Equal(4, result!.Items.Count);
            Assert.False(result.HasMore);
        }
        using var scope = factory.Services.CreateScope();
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.Role, session.Role), new Claim("org_id", session.OrganizationId.ToString())
        ], "test"));
        var policy = await scope.ServiceProvider.GetRequiredService<IAuthorizationService>()
            .AuthorizeAsync(principal, null, "FinanceDecision");
        Assert.Equal(mayDecide, policy.Succeeded);
    }

    [Fact]
    public async Task Authenticated_requests_cannot_read_another_organization()
    {
        await using var factory = await IdentityFactory.CreateAsync();
        using var client = factory.HttpsClient();
        await LoginAsync(client, "admin");
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/organizations/{IdentityFactory.OtherId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/organizations/{IdentityFactory.OtherId}/members")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/organizations/{Guid.NewGuid()}")).StatusCode);
    }

    [Fact]
    public async Task Login_cannot_select_a_workspace_without_membership()
    {
        await using var factory = await IdentityFactory.CreateAsync();
        using var client = factory.HttpsClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(client, "employee", "other-labs")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/session")).StatusCode);
    }

    [Fact]
    public async Task Missing_and_forged_csrf_are_rejected()
    {
        await using var factory = await IdentityFactory.CreateAsync();
        using var client = factory.HttpsClient();
        var credentials = new LoginRequest("employee@northstar.example", DevelopmentSeed.Password, "northstar-labs");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/auth/login", credentials)).StatusCode);
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", "forged");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/auth/login", credentials)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/session")).StatusCode);
    }

    [Fact]
    public async Task Session_cookies_are_secure_and_logout_requires_fresh_csrf()
    {
        await using var factory = await IdentityFactory.CreateAsync();
        using var client = factory.HttpsClient();
        var login = await LoginAsync(client, "employee");
        var cookie = Assert.Single(login.Headers.GetValues("Set-Cookie"), value => value.StartsWith("__Host-ExpenseGuard.Session=", StringComparison.Ordinal));
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/api/auth/logout", null)).StatusCode);
        await SetCsrfAsync(client);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/auth/logout", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/session")).StatusCode);
    }

    [Fact]
    public async Task Revoked_membership_invalidates_an_existing_cookie()
    {
        await using var factory = await IdentityFactory.CreateAsync();
        using var client = factory.HttpsClient();
        await LoginAsync(client, "employee");
        var session = await client.GetFromJsonAsync<SessionResponse>("/api/auth/session");
        await using var db = factory.TenantDb(DevelopmentSeed.NorthstarId);
        var member = await db.Memberships.SingleAsync(m => m.UserId == session!.UserId);
        member.IsActive = false;
        await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/session")).StatusCode);
    }

    [Fact]
    public async Task Role_changes_invalidate_an_existing_cookie()
    {
        await using var factory = await IdentityFactory.CreateAsync();
        using var client = factory.HttpsClient();
        await LoginAsync(client, "admin");
        var session = await client.GetFromJsonAsync<SessionResponse>("/api/auth/session");
        await using var db = factory.TenantDb(DevelopmentSeed.NorthstarId);
        var member = await db.Memberships.SingleAsync(m => m.UserId == session!.UserId);
        member.Role = OrganizationRole.Employee;
        await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/session")).StatusCode);
    }

    [Fact]
    public async Task Repeated_bad_passwords_lock_the_account()
    {
        await using var factory = await IdentityFactory.CreateAsync();
        using var client = factory.HttpsClient();
        for (var attempt = 0; attempt < 5; attempt++)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(client, "employee", password: "incorrect")).StatusCode);
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(client, "employee")).StatusCode);
    }

    [Fact]
    public async Task Tenant_filters_and_write_guard_fail_closed_without_scope()
    {
        await using var factory = await IdentityFactory.CreateAsync();
        await using var missingScope = factory.TenantDb(null);
        Assert.Empty(await missingScope.Organizations.ToListAsync());
        Assert.Empty(await missingScope.Memberships.ToListAsync());
        missingScope.Organizations.Add(new Organization { Id = Guid.NewGuid(), Name = "Forbidden", Slug = "forbidden" });
        await Assert.ThrowsAsync<InvalidOperationException>(() => missingScope.SaveChangesAsync());

        await using var scoped = factory.TenantDb(DevelopmentSeed.NorthstarId);
        Assert.Single(await scoped.Organizations.ToListAsync());
        scoped.Memberships.Add(new Membership { OrganizationId = IdentityFactory.OtherId, UserId = Guid.NewGuid() });
        await Assert.ThrowsAsync<InvalidOperationException>(() => scoped.SaveChangesAsync());
    }

    [Fact]
    public async Task Seeder_is_idempotent()
    {
        await using var factory = await IdentityFactory.CreateAsync();
        await using var scope = factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<DevelopmentSeed>().SeedAsync();
        await using var db = factory.TenantDb(DevelopmentSeed.NorthstarId);
        Assert.Equal(4, await db.Memberships.CountAsync());
        Assert.Equal(4, await db.Users.CountAsync());
    }

    [Fact]
    public async Task Demo_accounts_cannot_login_in_production_even_when_database_is_reused()
    {
        await using var seeded = await IdentityFactory.CreateAsync();
        await using var production = new IdentityFactory("Production", seeded.Connection);
        using var client = production.HttpsClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(client, "employee")).StatusCode);
    }

    [Theory]
    [InlineData("Demo:Seed")]
    [InlineData("Database:Initialize")]
    public void Production_refuses_development_initialization(string flag)
    {
        using var factory = new IdentityFactory("Production", enabledFlag: flag);
        var error = Assert.Throws<InvalidOperationException>(() => factory.HttpsClient());
        Assert.Contains("Development initialization is forbidden", error.Message);
    }

    [Fact]
    public async Task Anonymous_data_access_is_denied_and_http_cannot_issue_csrf()
    {
        await using var factory = await IdentityFactory.CreateAsync();
        using var client = factory.HttpsClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"/api/organizations/{DevelopmentSeed.NorthstarId}")).StatusCode);
        using var http = factory.CreateClient();
        Assert.Equal(HttpStatusCode.UpgradeRequired, (await http.GetAsync("/api/auth/csrf")).StatusCode);
    }

    private static async Task SetCsrfAsync(HttpClient client)
    {
        var csrf = await client.GetFromJsonAsync<CsrfResponse>("/api/auth/csrf");
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf!.RequestToken);
    }

    private static async Task<HttpResponseMessage> LoginAsync(HttpClient client, string account,
        string workspace = "northstar-labs", string password = DevelopmentSeed.Password)
    {
        await SetCsrfAsync(client);
        return await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest($"{account}@northstar.example", password, workspace));
    }

    private sealed class IdentityFactory : WebApplicationFactory<Program>
    {
        public static readonly Guid OtherId = Guid.Parse("b1919e80-5f52-4ec9-a269-4b2f66dbb4d8");
        private readonly string environment;
        private readonly string? enabledFlag;
        private readonly bool ownsConnection;
        public SqliteConnection Connection { get; }

        public IdentityFactory(string environment = "Development", SqliteConnection? connection = null, string? enabledFlag = null)
        {
            this.environment = environment;
            this.enabledFlag = enabledFlag;
            ownsConnection = connection is null;
            Connection = connection ?? new SqliteConnection("Data Source=:memory:");
            if (ownsConnection) { Connection.Open(); }
        }

        public static async Task<IdentityFactory> CreateAsync()
        {
            var factory = new IdentityFactory();
            await using var scope = factory.Services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<ExpenseGuardDbContext>().Database.EnsureCreatedAsync();
            await scope.ServiceProvider.GetRequiredService<DevelopmentSeed>().SeedAsync();
            await using var other = factory.TenantDb(OtherId);
            other.Organizations.Add(new Organization { Id = OtherId, Name = "Other Labs", Slug = "other-labs" });
            await other.SaveChangesAsync();
            return factory;
        }

        public HttpClient HttpsClient() => CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });

        public ExpenseGuardDbContext TenantDb(Guid? id) => new(
            new DbContextOptionsBuilder<ExpenseGuardDbContext>().UseSqlite(Connection).Options, new FixedTenant(id));

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(environment);
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = "TestServer replaces this with SQLite.",
                ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "",
                ["Demo:Seed"] = enabledFlag == "Demo:Seed" ? "true" : "false",
                ["Database:Initialize"] = enabledFlag == "Database:Initialize" ? "true" : "false"
            }));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<ExpenseGuardDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<ExpenseGuardDbContext>>();
                services.AddDbContext<ExpenseGuardDbContext>(options => options.UseSqlite(Connection));
                services.AddDataProtection().UseEphemeralDataProtectionProvider();
                services.RemoveAll<IDependencyProbe>();
                services.AddSingleton<IDependencyProbe, OfflineProbe>();
            });
        }

        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
            if (ownsConnection) { await Connection.DisposeAsync(); }
        }

        private sealed record FixedTenant(Guid? OrganizationId) : ITenantContext;
        private sealed class OfflineProbe : IDependencyProbe
        {
            public Task<ProbeResult> CheckAsync(string dependency, CancellationToken cancellationToken) =>
                Task.FromResult(ProbeResult.Healthy);
        }
    }
}
