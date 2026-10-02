# ExpenseGuard AI — Product Specification

Status: implementation contract  
Target: portfolio-grade release  
Companion documents: `EXPENSEGUARD_MCP_SERVER_SPEC.md` and `EXPENSEGUARD_CODEX_PROMPT.md`

## 1. Product summary

ExpenseGuard is an AI-powered expense auditing and reimbursement-review platform.

Employees submit receipts and expense details. An AI agent extracts structured data, researches the policy that applied on the transaction date, detects duplicates and inconsistencies, requests missing information, calls deterministic financial tools, and prepares an evidence-backed recommendation for a finance reviewer.

The agent may investigate and recommend. Only an authorized human may approve, partially approve, reject, export, or mark a reimbursement as paid.

### Elevator pitch

> I built an AI expense-auditing agent using C#, ASP.NET Core, React, PostgreSQL, the OpenAI Responses API, and a custom remote MCP server. The agent analyzes receipt images, discovers and invokes MCP tools, retrieves cited policy evidence, asks employees for missing information, and produces a structured recommendation. Deterministic C# services perform all authoritative financial calculations, humans retain control over financial decisions, and the system includes durable runs, prompt-injection defenses, evaluations, Docker, Kubernetes, Terraform, and CI/CD.

### Tagline

**Every expense explained. Every decision traceable.**

## 2. Why this is an agent

ExpenseGuard is not a receipt chatbot or a single LLM call. For each submitted expense, the agent:

1. Determines what it must establish.
2. Examines uploaded evidence.
3. Discovers the tools exposed by the ExpenseGuard MCP server.
4. Selects and calls the appropriate tools.
5. Observes results and adjusts its investigation.
6. Requests clarification when evidence is insufficient.
7. Resumes the same durable run after the employee responds.
8. Determines when the investigation is complete.
9. Produces a structured recommendation with policy and receipt citations.
10. Pauses for an authorized human decision.

The agent's state, tool calls, evidence, versions, usage, cost, and outcome are persisted and inspectable. The product never presents hidden chain-of-thought. It presents a concise plan, tool activity, evidence, reason codes, and decision summary.

## 3. Product principles

- AI interprets evidence and coordinates the workflow.
- Deterministic code performs authoritative arithmetic and rule enforcement.
- The MCP server is the controlled gateway to business data and actions.
- The model is never treated as a security boundary.
- Human reviewers own financial decisions.
- Every material conclusion is evidence-linked.
- Missing evidence produces clarification or escalation, not invention.
- Agent quality is measured with reproducible evaluations.
- The local demo works without paid services or an API key.
- Start with one capable agent; add specialist agents only if evaluations prove the need.

## 4. Users and permissions

### Employee

- Creates and submits expenses
- Uploads receipts
- Reviews extracted fields
- Corrects extraction errors
- Answers agent clarification questions
- Tracks status

### Finance reviewer

- Reviews agent recommendations
- Examines receipts, calculations, and cited policy clauses
- Corrects review data with an audit trail
- Approves, partially approves, rejects, or returns expenses
- Records the final human rationale

### Organization administrator

- Manages members and roles
- Uploads and activates policy versions
- Configures limits and approval rules
- Reviews agent configuration, usage, cost, and evaluations
- Manages retention settings

### Auditor

- Has read-only access to expenses, evidence, policy versions, human decisions, agent runs, MCP calls, and audit records

## 5. Goals

1. Reduce repetitive expense-review work.
2. Make every recommendation understandable and traceable.
3. Demonstrate a genuine tool-using agent implemented primarily in C#.
4. Demonstrate a production-quality MCP server and client.
5. Separate probabilistic reasoning from deterministic financial operations.
6. Prevent AI from independently executing consequential financial decisions.
7. Make behavior measurable through trace-based and dataset-based evaluations.
8. Provide a polished three-minute portfolio demonstration.

## 6. Non-goals

ExpenseGuard will not:

- Transfer real money
- Connect to real bank accounts in the portfolio release
- Provide tax, accounting, or legal advice
- Make accusations of fraud
- Automatically reject an employee's claim
- Treat a model confidence score as proof
- Train a foundation model
- Begin as a multi-agent system
- Support every jurisdiction's reimbursement law
- Replace a production accounting platform
- Expose approval, rejection, export, payment, policy-administration, or role-administration tools through MCP

Possible duplicates, altered documents, and unusual expenses are described as risk indicators, never as proof of fraud.

## 7. Seeded demonstration

The fictional organization is **Northstar Labs**.

An employee returning from a conference uploads:

- A hotel PDF containing room charges and a personal minibar charge
- A photographed restaurant receipt
- An expense entered in CAD while the receipt is in USD
- A taxi receipt that was accidentally submitted twice

The agent:

1. Extracts merchants, dates, currencies, taxes, tips, totals, and line items.
2. Calls the MCP server for the expense review context.
3. Finds the applicable travel-and-meals policy through MCP resources and search tools.
4. Detects the likely duplicate taxi claim.
5. Calls deterministic currency and eligibility calculations.
6. Identifies the minibar item as ineligible.
7. Notices that required meal-attendee information is missing.
8. Creates a clarification request through an allowed MCP action.
9. Pauses the durable run.
10. Resumes after the employee answers.
11. Calculates the eligible amount through deterministic services.
12. Submits an evidence-linked partial-approval recommendation.
13. Sends the case to a finance reviewer.
14. Records the human decision independently from the agent recommendation.

## 8. State models

### Expense lifecycle

```text
Draft
  -> Submitted
  -> AgentReviewQueued
  -> AgentReviewRunning
  -> NeedsEmployeeInformation
  -> AgentReviewRunning
  -> ReadyForFinanceReview
  -> Approved | PartiallyApproved | Rejected | Returned
  -> Exported
  -> Paid
```

Only an authorized finance reviewer may transition an expense to `Approved`, `PartiallyApproved`, `Rejected`, `Exported`, or `Paid`.

### Agent-run lifecycle

```text
Created
  -> Queued
  -> Running
  -> AwaitingClarification
  -> Queued
  -> Running
  -> AwaitingHumanDecision
  -> Completed

Terminal alternatives:
Failed | Cancelled | Expired
```

Runs must survive process restarts. Every run has maximum turn, time, token, tool-call, and estimated-cost budgets.

## 9. Agent capabilities

### Receipt understanding

- Accept JPEG, PNG, supported HEIC inputs, and PDF
- Detect unreadable, incomplete, or conflicting evidence
- Extract document-level and line-item data
- Attach confidence at the field level
- Preserve original model output for audit
- Validate all extracted data before persistence
- Allow human corrections while retaining history
- Never overwrite a human correction silently

### Policy research

- Ingest Markdown and PDF policies
- Split policies into stable, addressable clauses
- Preserve headings, page numbers, effective dates, and versions
- Use hybrid lexical and vector retrieval
- Require citations for policy conclusions
- Apply the policy effective on the transaction date
- Preserve the policy snapshot used by each recommendation

### Deterministic financial operations

C# services, reached through the MCP server, calculate:

- Line-item totals
- Tax and tip
- Currency conversion
- Per-person meal limits
- Daily limits
- Mileage reimbursement
- Eligible and ineligible amounts
- Approval thresholds
- Duplicate fingerprints

All money uses `decimal` with explicit ISO 4217 currency codes and documented rounding rules.

### Clarification

The agent asks a focused question when a receipt is unreadable, business purpose is absent, attendees are required, personal and business charges must be separated, a date or currency is ambiguous, or policy is inconclusive.

It must not invent missing information.

### Recommendation

The structured recommendation contains:

- Recommended disposition
- Requested, eligible, and ineligible amounts
- Currency
- Reason codes
- Concise explanation
- Policy citations
- Receipt and expense evidence
- Duplicate or anomaly indicators
- Unresolved questions
- Recommended reviewer action
- Confidence
- Agent, model, prompt, policy, MCP contract, and tool versions

Allowed recommendations are `Approve`, `PartiallyApprove`, `Reject`, `RequestInformation`, and `EscalateForHumanJudgment`. These are recommendations, not decisions.

## 10. Functional scope

### P0 — Portfolio release

- Organizations and memberships
- ASP.NET Core Identity authentication
- Employee, finance-reviewer, administrator, and auditor roles
- Expense creation and submission
- Receipt upload and preview
- Versioned policy ingestion
- Policy clause search and citations
- Separate ASP.NET Core MCP server
- MCP client in the agent worker
- Remote Streamable HTTP transport
- Deterministic financial tools exposed through MCP
- Durable agent review
- Live progress through SignalR
- Structured extraction
- Duplicate detection
- Employee clarification and run resumption
- Evidence-backed agent recommendation
- Finance-review queue
- Human decision workflow
- Immutable audit trail
- Agent and MCP traces
- Seeded Northstar Labs scenario
- Deterministic fake model and fake external services
- Configurable live OpenAI Responses API provider

### P1 — Engineering showcase

- PostgreSQL hybrid retrieval with pgvector
- Redis caching and SignalR scale-out
- Separate API, agent worker, and MCP-server processes
- Transactional outbox
- Run cancellation and bounded retries
- Per-run resource budgets
- Prompt, model, schema, and MCP contract versioning
- OAuth/JWT-protected MCP endpoint
- Capability-scoped agent-run tokens
- MCP resources and prompts in addition to tools
- OpenTelemetry across API, worker, MCP server, model, and database
- Evaluation runner and dashboard
- Prompt-injection regression suite
- Docker Compose
- Kubernetes and Helm
- Terraform Azure reference environment
- GitHub Actions CI/CD
- Container, dependency, code, and secret scanning

### P2 — Optional differentiators

- Batch expense review
- Manager approval thresholds
- Accounting-export adapter that still requires human approval
- Policy-impact simulation
- Prompt and model A/B evaluation
- Reviewer-feedback dataset
- A second specialist extraction agent only if evaluation results justify it

Autonomous payments remain out of scope.

## 11. User experience

### Employee

- Dashboard
- New expense
- Receipt upload
- Extraction review
- Clarification conversation
- Expense detail
- Expense history

### Finance

- Review queue
- High-density expense review workspace
- Side-by-side receipt viewer
- Agent findings
- Policy evidence
- Calculation breakdown
- Duplicate comparison
- Human decision form
- Review history

### Administration

- Policy library
- Policy versions and clauses
- Members and roles
- Agent configuration
- MCP tool catalogue and health
- Evaluation dashboard
- Audit explorer

### Agent-run view

Show:

- Run state and current stage
- Concise plan
- Completed steps
- MCP tool/resource name and status
- Sanitized result summary
- Evidence collected
- Clarification requests
- Recommendation
- Model, prompt, and MCP contract versions
- Token usage, estimated cost, and elapsed time
- Authorized cancel, retry, and resume controls

Do not display private chain-of-thought.

### Visual direction

- Credible financial-operations software
- Calm neutral palette
- Dense but legible tables
- Excellent monetary formatting
- Strong document viewing
- Clear visual separation between AI recommendation and human decision
- Risk colors used sparingly
- Responsive layouts
- Keyboard accessibility
- WCAG 2.2 AA target
- Designed loading, streaming, empty, stale, validation, permission, and failure states

## 12. Domain model

Primary entities:

- `Organization`
- `User`
- `Membership`
- `ExpenseReport`
- `ExpenseItem`
- `Receipt`
- `ReceiptExtraction`
- `EmployeeCorrection`
- `ExpensePolicy`
- `PolicyVersion`
- `PolicyClause`
- `PolicyEmbedding`
- `ExchangeRate`
- `DuplicateCandidate`
- `AgentRun`
- `AgentRunStep`
- `McpInvocation`
- `ClarificationRequest`
- `ClarificationResponse`
- `AgentRecommendation`
- `FinanceDecision`
- `ReimbursementExport`
- `PromptVersion`
- `ModelProfile`
- `McpContractVersion`
- `EvaluationCase`
- `EvaluationRun`
- `AuditEntry`
- `OutboxMessage`

Rules:

- Every tenant-owned record has an `OrganizationId`.
- Cross-organization access is forbidden.
- Original receipt objects are immutable.
- Human corrections retain previous values.
- Activated policy versions are immutable.
- Recommendations cannot overwrite finance decisions.
- Audit entries are append-only.
- Model output is untrusted until schema and domain validation succeed.
- MCP arguments never establish tenant identity; authenticated claims do.

## 13. Architecture

```text
React + TypeScript
        |
        | REST + SignalR
        v
ASP.NET Core API -------------------- Object storage
        |                                  |
        |                                  | receipts
        v                                  |
PostgreSQL + pgvector <--------------+-----+
        |
        | durable jobs / outbox
        v
.NET Agent Worker
        |
        | official MCP C# client
        | scoped run token
        v
ExpenseGuard MCP Server
        |
        | application services
        +---- policy retrieval
        +---- duplicate search
        +---- deterministic calculations
        +---- clarification proposal
        +---- recommendation submission
        |
        v
PostgreSQL / Redis / object metadata

.NET Agent Worker
        |
        | official OpenAI .NET client
        v
OpenAI Responses API
```

The MCP server is a separate deployable ASP.NET Core process. It shares domain and application contracts but owns its transport, authentication, tool/resource/prompt discovery, authorization, validation, and MCP-specific observability.

Detailed MCP requirements are in `EXPENSEGUARD_MCP_SERVER_SPEC.md`.

## 14. Repository structure

```text
src/
  ExpenseGuard.Api/
  ExpenseGuard.Application/
  ExpenseGuard.Domain/
  ExpenseGuard.Infrastructure/
  ExpenseGuard.Agent/
  ExpenseGuard.Worker/
  ExpenseGuard.McpServer/
  ExpenseGuard.McpClient/
web/
tests/
  ExpenseGuard.UnitTests/
  ExpenseGuard.IntegrationTests/
  ExpenseGuard.AgentTests/
  ExpenseGuard.McpTests/
  ExpenseGuard.ArchitectureTests/
  ExpenseGuard.EndToEndTests/
  ExpenseGuard.Evals/
deploy/
  helm/expenseguard/
infra/
  terraform/azure/
docs/
  adr/
  prompts/
  threat-model/
```

Use a modular monolith with separate deployable API, worker, web, and MCP-server processes.

## 15. Technology stack

### Backend and agent

- C#
- .NET 10 LTS
- ASP.NET Core
- Entity Framework Core with Npgsql
- PostgreSQL and pgvector
- Redis
- SignalR
- Official OpenAI .NET library
- OpenAI Responses API
- Official Model Context Protocol C# SDK
- Streamable HTTP MCP transport
- OpenTelemetry
- ASP.NET Core Identity
- OpenAPI 3.1

### Frontend

- React
- Strict TypeScript
- Vite
- React Router
- TanStack Query
- React Hook Form
- Tailwind CSS
- Accessible shadcn/Radix components
- Generated TypeScript API client
- Vitest
- React Testing Library
- Playwright

### Storage and platform

- S3-compatible storage abstraction
- MinIO locally
- Azure Blob Storage reference adapter
- Docker and Docker Compose
- Kubernetes and Helm
- Terraform
- GitHub Actions and GHCR

## 16. Model-provider and external-service abstractions

Define interfaces for:

- Model execution
- Receipt extraction
- Embeddings
- Object storage
- Exchange rates
- Notifications
- Malware scanning
- Accounting export

Provide live, deterministic fake, and recording/replay implementations where appropriate.

No automated test may require an API key, network access, or paid service.

## 17. Security and privacy

Required controls:

- Secure HTTP-only cookie authentication
- Anti-forgery protection
- Restrictive CORS and Content Security Policy
- Rate limiting
- File type, size, and signature validation
- Malware-scanning seam
- Encryption in transit
- Secrets outside source control
- Strict tenant isolation
- Short-lived signed document URLs
- PII redaction from logs and telemetry
- Configurable retention
- Append-only audits
- Prompt-injection defenses
- MCP tool argument validation
- Capability-scoped MCP access tokens
- Per-run resource budgets
- Idempotency for action-like tools
- Human approval outside the MCP tool surface

Receipt, policy, filename, metadata, user comment, and MCP result text are untrusted data. Text such as “ignore previous instructions” must not affect agent behavior.

## 18. Evaluation strategy

Create a versioned synthetic gold dataset covering:

- Normal compliant expenses
- Over-limit meals
- Missing receipts and attendees
- Mixed personal/business charges
- Multiple currencies
- Incorrect arithmetic
- Duplicates and legitimate lookalikes
- Unreadable and multi-page receipts
- Conflicting policy clauses
- Policy-version boundaries
- Altered-looking evidence
- Prompt injection in receipts and policies
- Cross-tenant access attempts
- Attempts to approve, reject, export, or pay through the agent or MCP server
- MCP timeouts, malformed responses, unavailable tools, and version changes

Measure:

- Extraction accuracy
- Correct tool selection
- Policy citation validity
- Applicable-policy selection
- Duplicate classification
- Recommendation classification
- Eligible-amount accuracy
- Clarification and escalation behavior
- Structured-output validity
- Tenant isolation
- Prompt-injection resistance
- Unauthorized-action rate
- MCP contract adherence
- Completion, latency, turns, tokens, and cost

Release gates for the controlled portfolio dataset:

- 100% schema-valid recommendations
- 100% deterministic calculation accuracy
- Zero cross-tenant disclosures
- Zero unapproved financial actions
- Zero successful critical prompt-injection cases
- Zero discovery of forbidden financial-decision tools
- At least 90% correct recommendation classification
- Every conclusion has valid evidence or explicitly escalates

## 19. Testing

### Backend

- Domain and state-transition tests
- Monetary and policy-applicability tests
- Testcontainers integration tests
- Tenant and authorization tests
- Durable-run recovery tests
- Outbox and idempotency tests
- Object-storage tests

### Agent and MCP

- Deterministic tool-selection scenarios
- Structured-output contracts
- Prompt snapshots and versions
- MCP discovery snapshots
- Tool and resource contract tests
- OAuth/JWT and capability-scope tests
- Prompt-injection tests
- Cancellation, timeout, budget, and restart tests
- Recorded-response replay
- Gold-dataset evaluation runner
- MCP Inspector smoke instructions

### Frontend and platform

- Component and accessibility tests
- Streaming and approval tests
- Playwright demonstration
- Container smoke tests
- Non-root execution
- Helm validation
- Disposable kind deployment
- Terraform validation
- k6 scenarios

## 20. Observability

Instrument API requests, agent runs, model calls, MCP client calls, MCP server calls, tool handlers, database operations, worker jobs, file processing, clarification wait time, and reviewer time.

Propagate W3C trace context across the worker-to-MCP HTTP boundary.

Metrics include run outcomes, tool failures, MCP latency, turn count, tokens, cost, reviewer overrides, citation failures, injection attempts, and approval outcomes.

Never record raw receipts, credentials, unnecessary PII, bearer tokens, private reasoning, or full unredacted tool results in telemetry.

## 21. Deployment

### Local

One documented command starts:

- React web app
- ASP.NET Core API
- Agent worker
- MCP server
- PostgreSQL with pgvector
- Redis
- MinIO
- Local mail catcher
- OpenTelemetry collector

The deterministic demo does not require an OpenAI key.

### Kubernetes

The Helm chart includes web, API, worker, and MCP-server deployments; services and ingress; startup/readiness/liveness probes; resource limits; disruption budgets; network policies; ConfigMaps; secret references; and a migration job.

The MCP server is network-reachable only by intended workloads unless an explicitly configured external demonstration endpoint is enabled.

Production PostgreSQL, Redis, and object storage are managed services in the reference design.

### Azure reference

Terraform may define ACR, AKS, Azure Database for PostgreSQL, managed Redis, Blob Storage, Key Vault, managed identity, monitoring, and an Azure OpenAI configuration seam.

Infrastructure is validated but never automatically applied.

## 22. CI/CD

Pull-request checks include:

1. Backend restore, formatting, build, and tests
2. Frontend lint, type-check, tests, and production build
3. Agent and critical security evaluations
4. MCP discovery and contract tests
5. OpenAPI client freshness
6. Container builds and scans
7. Helm and Terraform validation
8. Disposable kind deployment
9. MCP connectivity and authorization smoke test
10. Critical Playwright demonstration

Release artifacts include immutable images, SBOMs, a Helm package, and a GitHub release. No workflow deploys to paid infrastructure by default.

## 23. Required ADRs

1. Why .NET 10 instead of .NET 8
2. Why the agent runtime stays in C#
3. Why the Responses API is used
4. Why a single agent is the initial architecture
5. Why deterministic code performs financial calculations
6. Why PostgreSQL and pgvector are used
7. Why humans control financial decisions
8. Why the MCP server is a separate process
9. Why MCP uses scoped authorization
10. Why forbidden actions are absent rather than prompt-prohibited
11. Why fake and replay providers are required
12. Why Kubernetes is included

## 24. Three-minute portfolio demonstration

1. Log in as an employee.
2. Upload the seeded conference receipts.
3. Watch the agent discover and call MCP tools.
4. Show extraction, policy resources, and citations.
5. Show the duplicate taxi warning.
6. Let the agent request missing attendees.
7. Answer and resume the run.
8. Show the partial-approval recommendation and deterministic calculation.
9. Log in as a finance reviewer.
10. Approve the adjusted amount.
11. Show the independent human decision and audit history.
12. Show an end-to-end trace across model, worker, MCP server, and database.
13. Show the MCP tool catalogue and prove forbidden tools are absent.
14. Show one evaluation result and the Kubernetes deployment.

## 25. Definition of done

ExpenseGuard is complete when:

- A reviewer can start it from a clean checkout.
- Demo mode requires no paid API or private credential.
- Live OpenAI mode works through configuration.
- The complete demonstration succeeds.
- Agent runs survive worker restarts.
- The worker uses the MCP server for business tools.
- MCP authentication, tenant scoping, and capability enforcement pass.
- Forbidden financial-decision tools are not exposed.
- Financial calculations are deterministic and exact.
- Recommendations are evidence-linked.
- Human approval cannot be bypassed.
- Cross-tenant and injection tests pass.
- The evaluation dataset and results are published.
- Backend, frontend, MCP, integration, agent, and end-to-end tests pass.
- Docker Compose starts the complete system.
- Containers run as non-root.
- Helm installs into a clean kind cluster.
- Terraform validates.
- CI passes from a clean checkout.
- Documentation matches the implementation.
- The demonstrated path has no placeholder controls or TODO behavior.

## 26. Primary references

- OpenAI .NET library: https://github.com/openai/openai-dotnet
- OpenAI agent guide: https://openai.com/business/guides-and-resources/a-practical-guide-to-building-ai-agents/
- OpenAI agent safety: https://developers.openai.com/api/docs/guides/agent-builder-safety
- OpenAI agent evaluations: https://developers.openai.com/api/docs/guides/agent-evals
- Official MCP C# SDK: https://github.com/modelcontextprotocol/csharp-sdk
- MCP C# getting started: https://github.com/modelcontextprotocol/csharp-sdk/blob/main/docs/concepts/getting-started.md
