using ExpenseGuard.Domain;

namespace ExpenseGuard.UnitTests;

public sealed class AgentRunStateTests
{
    [Theory]
    [InlineData(AgentRunState.Completed)]
    [InlineData(AgentRunState.Cancelled)]
    [InlineData(AgentRunState.Expired)]
    [InlineData(AgentRunState.Failed)]
    public void Terminal_states_cannot_be_treated_as_active(AgentRunState state) => Assert.True(state.IsTerminal());

    [Theory]
    [InlineData(AgentRunState.Created)]
    [InlineData(AgentRunState.Queued)]
    [InlineData(AgentRunState.Running)]
    [InlineData(AgentRunState.AwaitingClarification)]
    [InlineData(AgentRunState.AwaitingHumanDecision)]
    public void Waits_preserve_an_incomplete_run(AgentRunState state) => Assert.False(state.IsTerminal());
}
