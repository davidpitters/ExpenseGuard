namespace ExpenseGuard.Application.SystemStatus;

public sealed record ComponentSummary(string Id, string Name, string Status, string Detail);

public sealed record SystemSummary(
    string Product,
    string Version,
    string Milestone,
    string ModelMode,
    string FinancialAuthority,
    DateTimeOffset CheckedAt,
    IReadOnlyList<ComponentSummary> Components);
