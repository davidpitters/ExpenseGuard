# MCP boundary

Official C# SDK 2.2.0; `ModelContextProtocol.AspNetCore` hosts Streamable HTTP in a separate process. The client uses the same SDK family. Stateless protocol sessions keep workflow state in PostgreSQL. The negotiated revision will be saved per agent run; do not assume a revision from a package version.

Milestone 0 mounts `/mcp` behind an unconditional deny policy. No business tools, resources, prompts, or sampling are registered. Health endpoints do not grant protocol access. Discovery and business behavior below are planned milestone-5 contracts, not implemented capabilities.

| Tool | Required scope | Side effect |
| --- | --- | --- |
| expenses_get_review_context | expense.review.read | None |
| receipts_get_extraction | expense.review.read | None |
| policies_search | policy.read | None |
| policies_get_clause | policy.read | None |
| expenses_find_duplicate_candidates | duplicate.read | None |
| finance_calculate_eligibility | finance.calculate | Versioned calculation record |
| clarifications_create_request | clarification.create | Idempotent clarification / wait |
| recommendations_submit | recommendation.create | Idempotent recommendation / human queue |

Resources: expense review context and evidence manifest, receipt extraction, immutable policy clause, agent-run tool manifest. Prompt: versioned `expense_review`. Each handler must validate audience/expiration/signature, unique organization/expense/run claims, allowed scopes and current database binding. No tenant argument can override a claim. Out-of-scope access cannot reveal existence.

Approval, partial approval, rejection, export, payment, policy administration, membership/role changes, arbitrary SQL/shell/URLs/email and unrestricted object access never appear in discovery. No worker bypass of this boundary is allowed.

Inputs and outputs are strict, bounded, versioned DTOs. Raw receipt bytes are never returned. Source text is labelled untrusted. Action keys store normalized payload hash and result; mismatched reuse conflicts. Safe typed errors and cancellation are required. Telemetry records identifiers, timing and outcomes, never arbitrary arguments/results or credentials.

MCP Inspector's authenticated discovery smoke procedure will be added with the authorized tool catalogue in milestone 5. Today a request to `/mcp` must return 401; an empty locked endpoint is not evidence of a completed MCP subsystem.
