using ExpenseGuard.McpServer;
using ExpenseGuard.ServiceDefaults;
using Microsoft.AspNetCore.Authentication;
using ModelContextProtocol.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.AddExpenseGuardDefaults("expenseguard-mcp");
builder.Services.AddAuthentication("Locked")
    .AddScheme<AuthenticationSchemeOptions, LockedAuthenticationHandler>("Locked", _ => { });
builder.Services.AddAuthorization(options =>
    options.AddPolicy("McpRun", policy => policy.RequireAuthenticatedUser().RequireAssertion(_ => false)));
builder.Services.AddMcpServer().WithHttpTransport(options =>
    options.SessionMode = HttpServerSessionMode.Stateless);

var app = builder.Build();
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.MapExpenseGuardHealth();
app.MapMcp("/mcp").RequireAuthorization("McpRun");
app.Run();

public partial class Program;
