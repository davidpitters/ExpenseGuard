using ExpenseGuard.Application.SystemStatus;
using ExpenseGuard.ServiceDefaults;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);
builder.AddExpenseGuardDefaults("expenseguard-api");
builder.Services.AddOpenApi(options => options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1);

var app = builder.Build();
app.UseExceptionHandler();
app.MapExpenseGuardHealth();

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
