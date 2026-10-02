namespace ExpenseGuard.Domain;

public enum AgentRunState
{
    Created,
    Queued,
    Running,
    AwaitingClarification,
    AwaitingHumanDecision,
    Completed,
    Failed,
    Cancelled,
    Expired
}

public static class AgentRunStates
{
    public static bool IsTerminal(this AgentRunState state) =>
        state is AgentRunState.Completed or AgentRunState.Failed
            or AgentRunState.Cancelled or AgentRunState.Expired;
}
