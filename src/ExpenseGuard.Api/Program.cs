using ExpenseGuard.Api.Identity;
using ExpenseGuard.Application.SystemStatus;
using ExpenseGuard.Infrastructure;
using ExpenseGuard.Infrastructure.Identity;
using ExpenseGuard.ServiceDefaults;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);
builder.AddExpenseGuardDefaults("expenseguard-api");
builder.AddExpenseGuardIdentity();
builder.Services.AddOpenApi(options => options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1);

var app = builder.Build();
if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Test")
    && (app.Configuration.GetValue<bool>("Demo:Seed") || app.Configuration.GetValue<bool>("Database:Initialize")))
{
    throw new InvalidOperationException("Development initialization is forbidden in this environment.");
}
app.UseExceptionHandler();
app.UseForwardedHeaders();
app.Use(async (context, next) =>
{
    try { await next(context); }
    catch (NpgsqlException) when (!context.Response.HasStarted)
    {
        app.Logger.LogWarning("Identity storage is unavailable.");
        await Results.Problem(statusCode: 503, title: "Identity storage is unavailable.")
            .ExecuteAsync(context);
    }
});
app.UseAuthentication();
app.UseAuthorization();
app.MapIdentityEndpoints();
app.MapExpenseGuardHealth();

if (builder.Configuration.GetValue<bool>("Database:Initialize"))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<ExpenseGuardDbContext>().Database.MigrateAsync();
}
if (builder.Configuration.GetValue<bool>("Demo:Seed"))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<DevelopmentSeed>().SeedAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapGet("/api/system", async (IDependencyProbe probe, TimeProvider clock, CancellationToken cancellationToken) =>
    {
        var definitions = new[]
        {
            ("worker", "Agent worker"), ("mcp", "MCP server"),
            ("postgres", "PostgreSQL + pgvector"), ("redis", "Redis"), ("object-storage", "Object storage")
        };
        var checks = await Task.WhenAll(definitions.Select(async definition =>
        {
            var result = await probe.CheckAsync(definition.Item1, cancellationToken);
            return new ComponentSummary(definition.Item1, definition.Item2, result.Status, result.Detail);
        }));
        return new SystemSummary("ExpenseGuard", "0.1.0", "0 — Executable foundation",
            "Disabled · no model calls", "Human finance reviewer", clock.GetUtcNow(),
            [new ComponentSummary("api", "Application API", "healthy", "HTTP request served."), .. checks]);
    }).WithName("GetSystemSummary").WithSummary("Development-only local system readiness.");
}

app.Run();

public partial class Program;
