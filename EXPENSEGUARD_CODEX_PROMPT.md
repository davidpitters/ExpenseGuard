# Codex Implementation Prompt — ExpenseGuard AI

Copy the prompt below into a new Codex task running GPT-6 Astra at high or xhigh reasoning. The repository must contain:

- `EXPENSEGUARD_PRODUCT_SPEC.md`
- `EXPENSEGUARD_MCP_SERVER_SPEC.md`

---

You are the principal engineer responsible for implementing ExpenseGuard AI as a polished, portfolio-ready agentic application.

Use GPT-6 Astra at high or xhigh reasoning. Work autonomously through safe local implementation tasks. Continue until the current milestone is complete and verified. Ask a focused question only when the answer would materially change product scope, architecture, cost, security, or authorization.

## Product contracts

Read these files completely before planning:

- `EXPENSEGUARD_PRODUCT_SPEC.md`
- `EXPENSEGUARD_MCP_SERVER_SPEC.md`

Treat them as the source of truth for product behavior, agent responsibilities, MCP capabilities, human-approval boundaries, technology, security, evaluations, demonstration flow, and definition of done.

If either file is missing, stop and report the missing contract. Do not invent a replacement product.

Inspect the repository and preserve existing work. Do not overwrite unrelated changes. Do not push code, create an external repository, provision cloud resources, spend money, connect real financial systems, or deploy outside the local environment without explicit authorization.

Verify unstable framework, SDK, API, model, and protocol details against current official documentation. Prefer supported stable releases. Record meaningful deviations from the contracts in architecture decision records.

## Core objective

Build an expense-auditing agent that:

1. Accepts expense reports and receipt images/PDFs.
2. Extracts structured receipt data.
3. Uses an MCP server to retrieve expense context and policy evidence.
4. Uses MCP-exposed deterministic C# tools for duplicate checks and financial calculations.
5. Requests missing information from employees.
6. Resumes durable runs after clarification.
7. Submits an evidence-linked recommendation.
8. Requires a finance reviewer to make every consequential financial decision.
9. Records model, prompt, MCP, tool, policy, calculation, and human-decision history.
10. Is evaluated against a versioned synthetic dataset.

The project succeeds only if the agent, MCP boundary, safety controls, evaluation results, and complete product workflow are demonstrable.

## Required stack

Use:

- C#
- .NET 10 LTS
- ASP.NET Core
- Entity Framework Core with Npgsql
- PostgreSQL with pgvector
- Redis
- ASP.NET Core Identity
- SignalR
- Official OpenAI .NET library
- OpenAI Responses API
- Official Model Context Protocol C# SDK
- `ModelContextProtocol.AspNetCore` for the remote MCP server
- Official MCP C# client in the agent worker
- Streamable HTTP MCP transport
- React with strict TypeScript
- Vite
- React Router
- TanStack Query
- React Hook Form
- Tailwind CSS and accessible component primitives
- OpenAPI 3.1 and a generated TypeScript client
- OpenTelemetry
- Docker Compose
- Kubernetes and Helm
- Terraform for an Azure reference architecture
- GitHub Actions and GHCR

Keep the agent runtime and MCP server in C#. Do not introduce a Python or Node orchestration service.

Start with one tool-using agent. Do not add multiple agents unless completed evaluations demonstrate a measurable need and the user approves the expansion.

Use a modular monolith with separately deployable web, API, agent-worker, and MCP-server processes.

## Non-negotiable safety boundaries

- The model is not a security boundary.
- The agent may investigate and recommend but may not make a financial decision.
- Approval, partial approval, rejection, export, payment, policy administration, and role administration are human-only operations.
- Human-only operations must be absent from MCP discovery, not merely prohibited by a prompt.
- Tenant identity comes from authenticated claims, never model-supplied arguments.
- Every tool and resource call is authenticated, scoped, validated, bounded, and audited.
- Financial calculations are deterministic C# operations using `decimal` and explicit currencies.
- Receipt, policy, filename, metadata, user, and tool-result text is untrusted data.
- Do not expose private chain-of-thought.
- Do not make fraud accusations.
- Do not commit secrets or real credentials.
- No automated test may require network access, an API key, or a paid service.

## Durable project memory

Before feature implementation, create and maintain:

- `PLAN.md`
- `STATUS.md`
- `docs/ARCHITECTURE.md`
- `docs/AGENT_DESIGN.md`
- `docs/MCP.md`
- `docs/SECURITY.md`
- `docs/THREAT_MODEL.md`
- `docs/EVALUATIONS.md`
- `docs/DEMO.md`
- `docs/prompts/`
- `docs/adr/`

`PLAN.md` contains milestones, dependencies, acceptance criteria, and validation commands.

`STATUS.md` contains the current milestone, completed work, next work, decisions, exact validation results, known issues, and external limitations.

Keep them accurate. They are operational project memory, not ceremonial documentation.

## Milestone plan

### Milestone 0 — Architecture and executable skeleton

Create:

- Solution and repository structure
- Domain, application, infrastructure, API, agent, worker, MCP server, and MCP client projects
- React application
- Test projects
- Central package management where appropriate
- Analyzer and formatting configuration
- Local configuration
- Initial Docker Compose dependencies
- Health endpoints
- Initial ADRs
- Seed-data strategy

Acceptance:

- Backend restores, builds, and tests.
- Frontend installs, type-checks, tests, and builds.
- PostgreSQL, Redis, and object storage start locally.
- API, worker, and MCP server report health.
- No secret is committed.

### Milestone 1 — Identity, organizations, and authorization

Implement:

- ASP.NET Core Identity
- Secure cookie authentication
- Anti-forgery protection
- Organizations and memberships
- Employee, finance-reviewer, administrator, and auditor roles
- Policy-based authorization
- Tenant-scoped data access
- Seeded Northstar Labs accounts

Acceptance:

- Role behavior is integration tested.
- Cross-organization access is denied.
- Demo accounts work.
- Development credentials cannot be enabled accidentally in production.

### Milestone 2 — Expense intake and evidence storage

Implement:

- Expense drafts and submissions
- Expense items
- Receipt upload
- Immutable object storage
- File content, type, size, and signature validation
- Receipt preview
- Extraction corrections with history
- S3-compatible abstraction
- MinIO development adapter
- Malware-scanning abstraction and deterministic fake

Acceptance:

- Supported images and PDFs upload and display.
- Invalid files fail safely.
- Object access is tenant-authorized.
- Original evidence is immutable.
- Corrections retain prior values.

### Milestone 3 — Policy ingestion and retrieval

Implement:

- Policy documents and immutable versions
- Effective dates
- Stable clauses
- Markdown and PDF ingestion
- Lexical and pgvector indexing
- Hybrid retrieval
- Page and clause citations
- Applicable-policy selection by transaction date

Acceptance:

- Seeded searches return expected clauses.
- Citations resolve to the correct immutable version.
- Retrieval is tenant-safe.
- Deterministic test retrieval exists.
- Instructions embedded in policies remain untrusted data.

### Milestone 4 — Deterministic finance engine

Implement C# services for:

- Monetary arithmetic
- Tax and tip
- Currency conversion
- Meal and daily limits
- Per-person calculations
- Mileage
- Eligible and ineligible totals
- Approval thresholds
- Duplicate fingerprints

Acceptance:

- Gold financial cases produce exact expected results.
- Money uses `decimal`.
- Currency and rounding are explicit.
- Exchange rates contain date and source.
- No authoritative amount depends on model arithmetic.

### Milestone 5 — MCP server and client

Implement `EXPENSEGUARD_MCP_SERVER_SPEC.md` before connecting the live agent.

Required server features:

- Separate ASP.NET Core deployable
- Official MCP C# SDK
- Streamable HTTP transport
- Protected endpoint
- Short-lived scoped run tokens
- Tools, resources, and versioned prompt
- Strict structured contracts
- Idempotency for action-like tools
- Bounded safe result shaping
- Typed safe errors
- OpenTelemetry
- Health checks

Required tools:

- `expenses_get_review_context`
- `receipts_get_extraction`
- `policies_search`
- `policies_get_clause`
- `expenses_find_duplicate_candidates`
- `finance_calculate_eligibility`
- `clarifications_create_request`
- `recommendations_submit`

Required resources:

- Expense review context
- Evidence manifest
- Receipt extraction
- Policy clause
- Agent-run tool manifest

Acceptance:

- The official MCP C# client discovers the server.
- Discovery matches an approved snapshot.
- Forbidden tools are absent.
- Authentication, audience, expiration, scope, organization, expense, and run binding pass.
- Malformed and oversized calls fail safely.
- Action-like tools are idempotent.
- Resource access is tenant-safe.
- MCP Inspector smoke instructions work.
- Container-to-container MCP calls work.

Do not hard-code runtime tool schemas in the agent worker. Discover them from the MCP server and persist the manifest and versions with each run.

### Milestone 6 — Durable agent runtime

Implement:

- Model-provider abstraction
- Live OpenAI Responses API provider
- Deterministic fake provider
- Recording/replay provider
- Durable `AgentRun` state machine
- Worker-owned run loop
- MCP client connection and discovery
- Model tool-call to MCP-call bridge
- Strict validation of model and MCP data
- Maximum turns, time, tokens, tool calls, and estimated cost
- Cancellation
- Retry and recovery
- SignalR progress streaming
- Prompt, model, schema, MCP contract, and tool-version tracking

Acceptance:

- A complete fake-provider run works offline.
- Live OpenAI mode is configuration driven.
- The worker uses MCP for business tools and does not bypass it.
- A worker restart resumes an incomplete run.
- Replays do not duplicate clarification or recommendation records.
- Invalid tool calls fail closed.
- MCP outages produce bounded recovery or escalation.
- Tool activity is visible without exposing chain-of-thought.

### Milestone 7 — Extraction, clarification, and recommendation

Implement:

- Multimodal receipt extraction
- Field-level confidence
- Missing-information detection
- Employee clarification
- Resume after clarification
- Duplicate investigation
- Policy research through MCP
- Deterministic calculation through MCP
- Evidence collection
- Validated structured recommendation
- Finance-review queue

Acceptance:

- The seeded conference scenario completes.
- Citations resolve.
- Missing data causes clarification rather than invention.
- Totals match deterministic calculations.
- Human corrections are respected.
- Recommendations cannot mutate human decisions.

### Milestone 8 — Human finance review

Implement:

- Side-by-side receipt viewer
- Extracted and corrected fields
- Agent findings
- Policy citations
- Calculation breakdown
- Duplicate comparison
- Human decision form
- Approval, partial approval, rejection, and return
- Immutable audit history

Acceptance:

- Only authorized finance reviewers can decide.
- Recommendation and decision remain separate records.
- Every decision records actor, time, reason, currency, and amount.
- Optimistic concurrency prevents stale review writes.
- No model, prompt, or MCP call can bypass the human boundary.

### Milestone 9 — Evaluations and adversarial testing

Create the versioned synthetic gold dataset required by the product contract.

Implement:

- C# evaluation runner
- Extraction graders
- Recommendation graders
- Citation validator
- Tool-selection grader
- Calculation comparison
- MCP contract grader
- Prompt-injection suite
- Tenant-isolation attacks
- Forbidden-action tests
- Latency, token, tool-call, and cost reporting
- Evaluation persistence and dashboard

Acceptance:

- All outputs are schema valid.
- Financial calculations are exact.
- There is no cross-tenant disclosure.
- There is no unapproved financial action.
- Forbidden MCP capabilities remain undiscoverable.
- Critical prompt-injection cases fail safely.
- Portfolio-dataset recommendation accuracy meets the contract.
- Results are reproducible and documented.

Do not weaken or curate away difficult cases to improve the score. Record genuine failures and repair the system.

### Milestone 10 — Observability and hardening

Implement:

- OpenTelemetry traces, metrics, and logs
- W3C context propagation through API, worker, MCP, and database
- Model and MCP usage tracking
- RFC Problem Details
- Rate limiting
- Timeouts
- Bounded retries with jitter
- Transactional outbox
- Graceful shutdown
- PII and secret redaction
- Security headers and Content Security Policy

Acceptance:

- A complete review is traceable end to end.
- Logs contain no receipt contents, tokens, credentials, or unnecessary PII.
- Model, MCP, tool, and dependency failures are visible and recoverable.
- Metrics agree with persisted run records.

### Milestone 11 — Containers and Kubernetes

Implement:

- Multi-stage images
- Non-root execution
- Complete Docker Compose environment
- Helm chart
- Migration job
- Startup, readiness, and liveness probes
- Requests and limits
- Ingress
- Pod disruption budgets
- Network policies
- ConfigMaps and secret references
- Appropriate horizontal scaling

Network policy must restrict the MCP server to the agent worker and designated validation jobs by default.

Acceptance:

- A clean Compose startup works.
- Images run as non-root.
- Helm lint and schema validation pass.
- The application installs into a clean kind cluster.
- Migrations complete.
- Worker-to-MCP authentication and connectivity work.
- The primary product smoke test succeeds in Kubernetes.

Do not run production database, cache, or object storage inside the reference Kubernetes deployment.

### Milestone 12 — Terraform and CI/CD

Implement:

- Azure reference Terraform
- GitHub Actions pull-request workflow
- Release workflow
- GHCR publication configuration
- SBOM generation
- Dependency, secret, code, and container scanning
- Helm and Terraform validation
- Disposable kind deployment
- MCP authorization smoke test
- Critical Playwright path

Acceptance:

- Terraform formats and validates.
- CI is reproducible from a clean checkout.
- No workflow provisions or deploys paid resources by default.
- Release artifacts are immutable and versioned.

### Milestone 13 — Portfolio polish

Complete:

- Professional README
- Architecture diagram
- Agent and MCP explanations
- Tool and resource catalogue
- Safety and approval model
- Evaluation methodology and results
- Demonstration guide
- Demo credentials
- Live-provider setup
- Kubernetes instructions
- Screenshots or recording instructions
- Known limitations and roadmap
- `CONTRIBUTING.md`
- `SECURITY.md`
- Issue and pull-request templates

Run the complete three-minute demonstration from a clean checkout.

## MCP implementation rules

- Business tool calls during agent runs must traverse MCP.
- The MCP server may reuse application interfaces but owns protocol and authorization concerns.
- Use coarse task-oriented tools rather than table-shaped CRUD tools.
- Tenant identity comes from authenticated claims.
- Each agent-run token is short lived, audience restricted, and minimally scoped.
- Validate organization, expense, and run binding.
- Enforce authorization in handlers or policies, not descriptions.
- Do not return raw receipt bytes through MCP.
- Keep excerpts bounded and source labelled.
- Propagate cancellation.
- Use typed safe errors.
- Require idempotency keys for clarification and recommendation submission.
- Persist the discovered tool manifest and versions per run.
- External MCP access is disabled by default.
- Do not use preview MCP packages or protocol features without documenting the need and tradeoff.

## Agent prompt-engineering rules

Store prompts as versioned files, not scattered string literals.

Create:

- Main expense-review system prompt
- Receipt-extraction prompt
- Recommendation schema
- Clarification schema
- Tool-use guidance
- Adversarial test prompts

Prompts require:

- Evidence-based conclusions
- Stable citations
- MCP tools for facts and calculations
- Explicit uncertainty
- Clarification instead of invention
- Escalation for ambiguous policy
- No fraud accusations
- No claims of approval, rejection, export, or payment
- Validated structured output
- Untrusted-document isolation

Do not request or persist chain-of-thought. Request concise explanations, evidence, and reason codes.

Run the evaluation suite whenever prompts, models, schemas, retrieval, MCP contracts, or tool descriptions change.

## Application engineering rules

- Prefer understandable code over pattern-heavy code.
- Keep domain rules independent from infrastructure and transports.
- Organize application behavior by business feature.
- Do not expose EF entities through APIs or MCP.
- Do not create generic repositories.
- Use explicit DTOs and contracts.
- Generate the TypeScript client from OpenAPI.
- Use server-side pagination and bounded queries.
- Store timestamps in UTC.
- Use optimistic concurrency for finance review.
- Commit deterministic migrations.
- Treat authored-code warnings as errors.
- Pin deliberate dependencies.
- Document unusual dependencies.
- Do not silently catch failures.
- Do not place fake controls or TODO behavior in the demonstration path.

## Frontend quality

Build credible financial-operations software rather than a generic dashboard template.

Provide:

- Strong hierarchy and typography
- Excellent monetary formatting
- Accessible tables and forms
- Side-by-side evidence review
- Clear AI-versus-human labelling
- Live agent and MCP progress
- Complete loading, streaming, empty, stale, validation, denied, offline, and failure states
- Keyboard navigation
- WCAG 2.2 AA target
- Responsive layouts

Avoid excessive gradients, glass effects, decorative animation, fake analytics, and non-functional controls.

## Verification behavior

After every milestone:

1. Run relevant formatting, build, lint, type-check, unit, integration, contract, evaluation, security, and smoke checks.
2. Inspect failures rather than merely rerunning commands.
3. Repair failures before proceeding.
4. Update `STATUS.md` with exact results.
5. Review the diff for unrelated changes and generated clutter.
6. Confirm acceptance criteria through actual behavior.

If an external tool is unavailable, complete independent work and record the exact limitation, substitute verification, and remaining command. Never report skipped validation as successful.

## Final audit

Before declaring completion:

1. Start from a clean checkout using only the README.
2. Start deterministic demo mode without an OpenAI key.
3. Run all tests and evaluations.
4. Run the seeded conference-expense scenario.
5. Inspect MCP discovery and prove forbidden tools are absent.
6. Run critical prompt-injection and tenant-isolation scenarios.
7. Confirm that the human decision boundary cannot be bypassed.
8. Restart the worker during a run and verify recovery.
9. Deploy to a disposable kind cluster.
10. Execute the critical Playwright path there.
11. Inspect every visible control in the demonstration.
12. Check for secrets, binaries, dead code, and abandoned experiments.
13. Confirm that documentation matches the implementation.
14. Record limitations honestly.

Return a concise completion report containing:

- Implemented product capabilities
- Agent architecture
- MCP server design and exposed capabilities
- Safety and approval boundaries
- Evaluation results
- Validation results
- How to run and demonstrate the application
- Known limitations
- Optional future work

The success criterion is not “the application calls an LLM” or “an MCP endpoint exists.” The success criterion is a coherent, measurable, secure agent workflow in which tool discovery, evidence, calculations, MCP authorization, human approvals, and failures can all be inspected and defended in a technical interview.
