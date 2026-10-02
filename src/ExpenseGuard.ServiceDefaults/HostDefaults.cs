using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace ExpenseGuard.ServiceDefaults;

public static class HostDefaults
{
    public static readonly IReadOnlyList<string> Dependencies = Array.AsReadOnly(new[] { "postgres", "redis", "object-storage" });

    public static void AddExpenseGuardDefaults(this WebApplicationBuilder builder, string serviceName)
    {
        builder.Logging.ClearProviders();
        builder.Logging.AddJsonConsole();
        builder.Services.AddProblemDetails();
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddHttpClient("health", client => client.Timeout = TimeSpan.FromSeconds(4))
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
        builder.Services.AddSingleton<IDependencyProbe, DependencyProbe>();
        var health = builder.Services.AddHealthChecks();
        foreach (var dependency in Dependencies)
        {
            health.Add(new HealthCheckRegistration(dependency,
                services => new DependencyHealthCheck(services.GetRequiredService<IDependencyProbe>(), dependency),
                HealthStatus.Unhealthy, ["ready"], TimeSpan.FromSeconds(4)));
        }

        var telemetry = builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(traces => traces.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation())
            .WithMetrics(metrics => metrics.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation());
        builder.Logging.AddOpenTelemetry();
        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
        {
            telemetry.WithTracing(traces => traces.AddOtlpExporter())
                .WithMetrics(metrics => metrics.AddOtlpExporter());
            builder.Logging.AddOpenTelemetry(options => options.AddOtlpExporter());
        }
    }

    public static void MapExpenseGuardHealth(this WebApplication app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = WriteHealthAsync
        });
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = WriteHealthAsync
        });
    }

    private static Task WriteHealthAsync(HttpContext context, HealthReport report) =>
        context.Response.WriteAsJsonAsync(new { status = report.Status.ToString() }, context.RequestAborted);

    private sealed class DependencyHealthCheck(IDependencyProbe probe, string dependency) : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken)
        {
            var result = await probe.CheckAsync(dependency, cancellationToken);
            return result.Status == "healthy" ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy(result.Detail);
        }
    }
}
