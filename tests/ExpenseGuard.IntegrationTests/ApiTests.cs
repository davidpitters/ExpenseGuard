using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExpenseGuard.Application.SystemStatus;
using ExpenseGuard.ServiceDefaults;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ExpenseGuard.IntegrationTests;

public sealed class ApiTests
{
    [Theory]
    [InlineData("Development", "/api/system", HttpStatusCode.OK)]
    [InlineData("Production", "/api/system", HttpStatusCode.NotFound)]
    [InlineData("Production", "/openapi/v1.json", HttpStatusCode.NotFound)]
    public async Task Diagnostics_are_development_only(string environment, string path, HttpStatusCode expected)
    {
        await using var factory = new ApiFactory(environment, ProbeResult.Healthy);
        using var client = factory.CreateClient();
        Assert.Equal(expected, (await client.GetAsync(path)).StatusCode);
    }

    [Fact]
    public async Task Liveness_does_not_mask_missing_dependencies()
    {
        await using var factory = new ApiFactory("Development", ProbeResult.Missing);
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
        var readiness = await client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, readiness.StatusCode);
        Assert.Equal("{\"status\":\"Unhealthy\"}", await readiness.Content.ReadAsStringAsync());

        var summary = await client.GetFromJsonAsync<SystemSummary>("/api/system");
        Assert.NotNull(summary);
        Assert.Equal(6, summary.Components.Count);
        Assert.All(summary.Components.Where(c => c.Id != "api"), c => Assert.Equal("not_configured", c.Status));
        Assert.Equal("Human finance reviewer", summary.FinancialAuthority);
    }

    [Fact]
    public async Task Healthy_dependencies_make_readiness_successful()
    {
        await using var factory = new ApiFactory("Development", ProbeResult.Healthy);
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/ready")).StatusCode);
    }

    [Fact]
    public async Task OpenApi_contract_is_31_and_contains_typed_system_response()
    {
        await using var factory = new ApiFactory("Development", ProbeResult.Healthy);
        using var client = factory.CreateClient();
        using var document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));
        Assert.StartsWith("3.1.", document.RootElement.GetProperty("openapi").GetString());
        Assert.Equal("GetSystemSummary", document.RootElement.GetProperty("paths")
            .GetProperty("/api/system").GetProperty("get").GetProperty("operationId").GetString());
        Assert.True(document.RootElement.GetProperty("components").GetProperty("schemas")
            .TryGetProperty("SystemSummary", out _));
    }

    [Fact]
    public async Task Unrecognized_host_is_rejected()
    {
        await using var factory = new ApiFactory("Development", ProbeResult.Healthy);
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Host = "attacker.invalid";
        Assert.Equal(HttpStatusCode.BadRequest, (await client.SendAsync(request)).StatusCode);
    }

    private sealed class ApiFactory(string environment, ProbeResult result) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(environment);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IDependencyProbe>();
                services.AddSingleton<IDependencyProbe>(new FixedProbe(result));
            });
        }
    }

    private sealed class FixedProbe(ProbeResult result) : IDependencyProbe
    {
        public Task<ProbeResult> CheckAsync(string dependency, CancellationToken cancellationToken) =>
            Task.FromResult(result);
    }
}
