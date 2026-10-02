# Agent design

One worker-owned C# loop coordinates investigation. It discovers MCP tools at run start and snapshots the negotiated protocol, SDK/server/tool contracts, schemas, prompt and model versions. Runtime tool schemas must come from discovery. Neither Worker nor Agent may reference business handlers.

The durable lifecycle is Created → Queued → Running → AwaitingClarification → Queued → Running → AwaitingHumanDecision → Completed. Failed, Cancelled, and Expired are terminal alternatives. Future run transactions persist steps, usage, leases and outbox messages before acknowledging work. Action retries reuse logical idempotency keys.

Only concise progress summaries, evidence, reason codes and sanitized tool activity are shown. Never request, store, or display private chain-of-thought. Receipt/policy/user/tool text is untrusted even when syntactically valid. Validate all proposed calls and structured outputs independently of model instructions.

Bound every run by turns, wall time, input/output tokens, tool calls and estimated cost. Unknown usage or pricing must not silently permit an unlimited run. Cancellation propagates through model, MCP, and storage calls. Retry only typed transient errors within the same budgets; ambiguous policy escalates.

Milestone 0 establishes validated budget configuration and the official MCP transport factory. The worker currently runs a supervised heartbeat, with no expense polling or LLM calls. Fake, recording/replay and live Responses implementations, durable jobs, and SignalR activity are milestone 6. Prompts in `prompts/` are design assets until that runtime is evaluated.
