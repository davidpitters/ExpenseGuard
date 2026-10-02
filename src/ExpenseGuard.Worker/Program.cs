using ExpenseGuard.Agent;
using ExpenseGuard.ServiceDefaults;
using ExpenseGuard.Worker;

var builder = WebApplication.CreateBuilder(args);
builder.AddExpenseGuardDefaults("expenseguard-worker");
builder.Services.AddOptions<AgentBudgetOptions>()
    .BindConfiguration(AgentBudgetOptions.SectionName)
    .Validate(options => options.IsValid(), "Agent budgets must be positive and bounded.")
    .ValidateOnStart();
builder.Services.AddSingleton<WorkerHeartbeat>();
builder.Services.AddHostedService<WorkerHeartbeatService>();
builder.Services.AddHealthChecks().AddCheck<WorkerHeartbeat>("worker-loop", tags: ["ready"]);

var app = builder.Build();
app.UseExceptionHandler();
app.MapExpenseGuardHealth();
app.Run();

public partial class Program;
