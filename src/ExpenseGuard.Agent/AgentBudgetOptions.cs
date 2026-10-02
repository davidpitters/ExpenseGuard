namespace ExpenseGuard.Agent;

public sealed class AgentBudgetOptions
{
    public const string SectionName = "Agent:Budget";
    public int MaxTurns { get; set; } = 12;
    public int MaxToolCalls { get; set; } = 32;
    public int MaxTokens { get; set; } = 30000;
    public int MaxDurationSeconds { get; set; } = 120;
    public decimal MaxEstimatedCostUsd { get; set; } = 1.00m;

    public bool IsValid() =>
        MaxTurns is > 0 and <= 100
        && MaxToolCalls is > 0 and <= 256
        && MaxTokens is > 0 and <= 1_000_000
        && MaxDurationSeconds is > 0 and <= 900
        && MaxEstimatedCostUsd is > 0 and <= 25;
}
