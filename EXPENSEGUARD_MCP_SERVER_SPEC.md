# ExpenseGuard MCP Server — Technical Specification

Status: implementation contract  
Companion documents: `EXPENSEGUARD_PRODUCT_SPEC.md` and `EXPENSEGUARD_CODEX_PROMPT.md`

## 1. Purpose

The ExpenseGuard MCP server is the controlled protocol boundary between AI agents and ExpenseGuard business capabilities.

It exposes a deliberately limited collection of tools, resources, and prompts. It is responsible for transport, discovery, authentication, tenant scoping, capability authorization, argument validation, idempotency, safe output shaping, observability, and protocol-level testing.

The MCP server must be useful to the ExpenseGuard agent worker and testable with standards-compatible MCP clients. It must not become an alternate route around the application's authorization or human-review rules.

## 2. Architectural position

```text
OpenAI Responses API
        ^
        | tool calls/results
        |
.NET Agent Worker
        |
        | official MCP C# client
        | Streamable HTTP
        | short-lived scoped token
        v
ExpenseGuard.McpServer
        |
        | application interfaces
        v
ExpenseGuard application/domain services
        |
        +-- PostgreSQL + pgvector
        +-- Redis
        +-- object metadata
```

The agent worker must consume ExpenseGuard business tools through MCP. It must not bypass MCP by calling the same business services directly during an agent run.

Human web and REST workflows continue to use the main API. The MCP server is not a replacement for the product API.

## 3. Technology

- C#
- .NET 10 LTS
- ASP.NET Core
- Official `ModelContextProtocol.AspNetCore` package
- Official MCP C# client package in `ExpenseGuard.McpClient`
- Streamable HTTP transport
- Stateless server mode unless a verified requirement needs protocol session state
- OAuth 2.0/JWT bearer protection
- OpenTelemetry
- JSON Schema-derived tool inputs and structured outputs

Use the latest stable official MCP C# SDK compatible with the chosen protocol revision. Do not adopt a preview protocol or package solely because it is newer. Record the selected SDK and negotiated protocol behavior in an ADR.

An optional stdio host may be provided for local inspection, but production architecture uses protected Streamable HTTP.

## 4. Trust model

The model, agent prompt, uploaded documents, MCP client arguments, resource URIs, filenames, and tool-result text are untrusted.

The MCP server trusts only:

- Successfully validated authentication
- Server-created claims
- Server-side authorization policies
- Validated application state
- Deterministic domain services

An organization identifier supplied in tool arguments is never authoritative. Organization and user scope come from the authenticated principal.

## 5. Authentication and authorization

### Production model

The agent worker obtains a short-lived, audience-restricted token for one agent run.

Recommended claims:

- `sub`: service or delegated actor identity
- `org_id`: organization scope
- `agent_run_id`: current run
- `expense_id`: optional expense restriction
- `scope`: allowed MCP capabilities
- `aud`: ExpenseGuard MCP server
- `exp`: short expiration
- `jti`: unique token identifier

Example scopes:

- `expense.review.read`
- `policy.read`
- `duplicate.read`
- `finance.calculate`
- `clarification.create`
- `recommendation.create`
- `mcp.resources.read`

The token must not contain scopes for approving, rejecting, exporting, paying, changing policy, or managing users.

### Enforcement

Every tool and resource handler performs:

1. Token validation
2. Audience and expiration validation
3. Organization extraction from claims
4. Agent-run binding
5. Expense binding when present
6. Required-scope validation
7. Database tenant filtering
8. Object-level authorization

Fail closed when identity, scope, or binding is missing or ambiguous.

### Local development

Provide a development token issuer or deterministic authentication handler usable only in Development and Test environments. Startup must fail if development authentication is enabled in a production environment.

## 6. MCP capability surface

Expose tools, resources, and a small number of prompts. Do not expose sampling or server-driven model calls unless a later requirement and threat review justify them.

### Tools

Use coarse, task-oriented tools rather than mirroring database tables.

#### `expenses_get_review_context`

Purpose: Return the bounded review context for one expense.

Includes:

- Expense metadata
- Employee and cost-center attributes relevant to policy
- Receipt metadata
- Existing extraction status
- Prior clarifications
- Applicable policy-version identifiers

Does not return unrelated employee data or raw receipt bytes.

Required scope: `expense.review.read`

#### `receipts_get_extraction`

Purpose: Return validated extracted fields, line items, confidence, human corrections, and evidence references for one receipt.

Required scope: `expense.review.read`

#### `policies_search`

Purpose: Hybrid-search the applicable policy version and return a small ranked set of stable clause references with excerpts and scores.

Inputs include query, policy version, and bounded result count.

Required scope: `policy.read`

#### `policies_get_clause`

Purpose: Resolve a stable clause identifier to its full approved text and citation metadata.

Required scope: `policy.read`

#### `expenses_find_duplicate_candidates`

Purpose: Return bounded possible duplicates with deterministic similarity factors.

The result must say “candidate” or “risk indicator,” never “fraud.”

Required scope: `duplicate.read`

#### `finance_calculate_eligibility`

Purpose: Perform authoritative calculations for the current expense using explicit inputs and the selected policy version.

Returns:

- Requested amount
- Eligible amount
- Ineligible amount
- Currency
- Rounding details
- Applied deterministic rules
- Line-item breakdown
- Calculation version

Required scope: `finance.calculate`

#### `clarifications_create_request`

Purpose: Create one focused employee clarification request and move the run into `AwaitingClarification`.

Requirements:

- Idempotency key
- Bounded question length
- Structured reason code
- Referenced missing fields
- Current agent-run binding

Required scope: `clarification.create`

#### `recommendations_submit`

Purpose: Persist a validated recommendation and move the expense to `ReadyForFinanceReview`.

This tool does not approve, reject, export, pay, or otherwise make the human decision.

Requirements:

- Idempotency key
- Structured recommendation schema
- Valid citations
- Deterministic calculation identifier
- Current run, prompt, model, policy, and MCP contract versions

Required scope: `recommendation.create`

### Forbidden tools

The following must not exist in MCP discovery:

- Approve expense
- Partially approve expense
- Reject expense
- Export reimbursement
- Mark reimbursement as paid
- Change policy
- Activate policy
- Change membership
- Change user role
- Read arbitrary receipt objects
- Execute arbitrary SQL
- Execute shell commands
- Fetch arbitrary URLs
- Send arbitrary email

Absence from the tool surface is a stronger boundary than telling the model not to call them.

## 7. Resources

Resources expose stable, read-only evidence. Resource handlers apply the same authentication and tenant controls as tools.

Recommended URI templates:

- `expenseguard://expenses/{expenseId}/review-context`
- `expenseguard://expenses/{expenseId}/evidence-manifest`
- `expenseguard://receipts/{receiptId}/extraction`
- `expenseguard://policies/{policyVersionId}/clauses/{clauseId}`
- `expenseguard://agent-runs/{agentRunId}/tool-manifest`

Resource responses include:

- Stable identifier
- Media type
- Version
- Source timestamp
- Sensitivity classification
- Bounded content

Never expose raw secrets, bearer tokens, unrestricted document downloads, or cross-tenant listings.

## 8. Prompts

Expose a versioned `expense_review` prompt template for standards-compatible MCP clients.

It supplies the public operating contract, not secret security controls. It should establish:

- Decision-support role
- Evidence and citation requirements
- Use of deterministic tools for calculations
- Clarification instead of invention
- Escalation when policy is ambiguous
- No fraud accusations
- No financial-decision claims
- Structured recommendation requirement

The ExpenseGuard worker may use an application-owned prompt version that incorporates this template. Authorization still resides in server code.

## 9. Tool contract rules

Every tool has:

- Stable machine name
- Concise description
- Explicit usage conditions
- Strict input schema
- Structured output schema
- Required authorization scope
- Idempotency classification
- Maximum input and output sizes
- Timeout
- Version
- Documented domain errors

Descriptions must help the model choose correctly without embedding security secrets or excessive procedural text.

Reject unknown properties in sensitive write-like tool arguments where supported.

Use bounded enums and identifiers rather than unconstrained free text whenever practical.

## 10. Safe result shaping

MCP outputs may contain text originating in receipts or policies. Wrap untrusted text in clearly typed fields and label its source.

Tool results must:

- Be valid structured data
- Be bounded in length
- Separate system-generated fields from source excerpts
- Include stable evidence identifiers
- Exclude credentials and unnecessary PII
- Avoid returning executable markup
- Avoid returning entire documents when clauses suffice
- Never include instructions for the agent outside defined data fields

The agent prompt must treat every result as data. The application must still validate subsequent tool calls independently.

## 11. Error model

Return safe, typed errors such as:

- `authentication_required`
- `insufficient_scope`
- `resource_not_found`
- `resource_out_of_scope`
- `validation_failed`
- `conflict`
- `idempotency_conflict`
- `dependency_unavailable`
- `calculation_failed`
- `rate_limited`
- `timeout`

Do not reveal whether an out-of-scope tenant resource exists. Do not return stack traces or connection details.

The agent worker decides whether to retry, clarify, escalate, or fail based on typed error codes and retryability metadata.

## 12. Idempotency and concurrency

`clarifications_create_request` and `recommendations_submit` require an idempotency key derived from the agent run and logical operation.

The server stores the key, normalized request hash, result, and timestamp. Repeating the same request returns the original result. Reusing a key with different arguments returns `idempotency_conflict`.

Use optimistic concurrency when a tool changes review state. A stale run cannot overwrite a newer human or agent state.

## 13. Versioning

Track:

- MCP SDK version
- Negotiated protocol revision
- Server application version
- Tool contract version
- Resource schema version
- Prompt version
- Calculation version

Breaking tool changes use a new tool or contract version rather than silently changing semantics.

Persist the discovered tool manifest and versions on each agent run so historical recommendations remain reproducible.

## 14. Observability

Propagate W3C trace context from agent worker to MCP server.

Record:

- Client identity
- Agent run
- Tool/resource name
- Contract version
- Duration
- Success/error category
- Retry count
- Bounded input/output sizes
- Authorization outcome
- Idempotency outcome

Do not log bearer tokens, raw receipts, policy bodies, arbitrary model arguments, unnecessary PII, or full tool outputs.

Metrics:

- Discovery success and latency
- Calls per capability
- Error and timeout rates
- Authorization denials
- Idempotent replays
- Contract-validation failures
- Result-size rejections
- Per-tool latency

## 15. Testing

### Contract tests

- Tool discovery matches an approved snapshot
- Forbidden tools are absent
- Schemas reject malformed input
- Outputs conform to documented schemas
- Resources resolve to the correct tenant and version
- Prompt discovery returns the correct version

### Authentication tests

- Missing, expired, wrong-audience, and malformed tokens fail
- Missing scope fails
- Wrong organization fails without leaking existence
- Wrong expense or run binding fails
- Development authentication cannot start in production

### Security tests

- Cross-tenant identifiers
- Prompt injection inside policy excerpts
- Prompt injection inside receipt extraction
- Oversized input and output
- Path and URI manipulation
- Duplicate idempotency keys
- Replay after token expiration
- Attempts to discover or invoke forbidden capabilities
- SSRF-like arbitrary URL attempts

### Reliability tests

- Cancellation propagation
- Handler timeouts
- Database unavailability
- Redis unavailability where non-authoritative
- Worker retry behavior
- MCP server restart
- Version mismatch
- Concurrent recommendation submission

### Interoperability

- Official MCP C# client
- MCP Inspector smoke test
- Container-to-container Streamable HTTP
- Kubernetes network and authentication smoke test

## 16. Deployment

The MCP server has its own:

- Project
- Container image
- Health endpoints
- Kubernetes deployment
- Kubernetes service
- Configuration
- Secret references
- Resource requests and limits
- Network policy
- OpenTelemetry service identity

Default Kubernetes policy permits inbound traffic only from the agent worker and designated test job.

An external MCP endpoint is disabled by default. If enabled for a portfolio demonstration, it requires production-grade authentication, restrictive allowed hosts/CORS as applicable, rate limits, TLS, and a minimal read-only or synthetic-data capability set.

## 17. Acceptance criteria

The MCP subsystem is complete when:

- The server is a separate ASP.NET Core deployable.
- The worker uses the official MCP C# client.
- The worker discovers tools rather than hard-coding their runtime schemas.
- Business tool calls during agent runs traverse MCP.
- Streamable HTTP works locally and in Kubernetes.
- Authentication and scoped authorization pass.
- Tenant, expense, and run binding pass.
- Forbidden financial-decision tools are absent.
- Tools and resources return bounded structured results.
- Action-like tools are idempotent.
- Trace context spans worker, MCP server, and database.
- Discovery, contract, security, reliability, and interoperability tests pass.
- MCP Inspector instructions are documented.
- The portfolio demonstration visibly shows MCP discovery and calls.

## 18. References

- Official MCP C# SDK: https://github.com/modelcontextprotocol/csharp-sdk
- C# SDK getting started: https://github.com/modelcontextprotocol/csharp-sdk/blob/main/docs/concepts/getting-started.md
- Protected MCP server sample: https://github.com/modelcontextprotocol/csharp-sdk/blob/main/samples/ProtectedMcpServer/README.md
- MCP resources documentation: https://github.com/modelcontextprotocol/csharp-sdk/blob/main/docs/concepts/resources/resources.md
