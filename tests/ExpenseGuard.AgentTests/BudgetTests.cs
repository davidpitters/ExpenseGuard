using ExpenseGuard.Agent;

namespace ExpenseGuard.AgentTests;

public sealed class BudgetTests
{
    [Fact]
    public void Defaults_are_bounded() => Assert.True(new AgentBudgetOptions().IsValid());

    [Theory]
    [InlineData(0, 32, 30000, 120, 1)]
    [InlineData(101, 32, 30000, 120, 1)]
    [InlineData(12, 0, 30000, 120, 1)]
    [InlineData(12, 257, 30000, 120, 1)]
    [InlineData(12, 32, 0, 120, 1)]
    [InlineData(12, 32, 1000001, 120, 1)]
    [InlineData(12, 32, 30000, 0, 1)]
    [InlineData(12, 32, 30000, 901, 1)]
    [InlineData(12, 32, 30000, 120, 0)]
    [InlineData(12, 32, 30000, 120, 26)]
    public void Invalid_budgets_fail_closed(int turns, int tools, int tokens, int seconds, int cost)
    {
        Assert.False(new AgentBudgetOptions
        {
            MaxTurns = turns,
            MaxToolCalls = tools,
            MaxTokens = tokens,
            MaxDurationSeconds = seconds,
            MaxEstimatedCostUsd = cost
        }.IsValid());
    }
}
