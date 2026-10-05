# ExpenseGuard implementation plan

Source contracts: `EXPENSEGUARD_PRODUCT_SPEC.md` and `EXPENSEGUARD_MCP_SERVER_SPEC.md`, read in full on 2026-10-02. Work is local only. No cloud provisioning, paid calls, external publishing, or financial connections are authorized.

## Current milestone: 1 — identity and tenancy (infrastructure acceptance pending)

- [x] Establish the .NET 10 solution, dependency directions, package pins, analyzers, and offline test harnesses.
- [x] Create separate API, worker, MCP server, and React hosts with honest health reporting.
- [x] Configure PostgreSQL/pgvector, Redis, and MinIO in local Docker Compose (runtime gate below).
- [x] Generate the strict TypeScript client from ASP.NET Core OpenAPI 3.1.
- [x] Verify backend restore/build/format/tests and frontend install/lint/typecheck/tests/build.
- [ ] Exercise all three hosts and the frontend. Validate Compose startup when a container engine is available.
- [x] Record exact results, limitations, and next work in STATUS.md.

Milestone 1:

- [x] Identity secure cookies, HTTPS antiforgery, lockout and session revocation.
- [x] Organizations, memberships, four roles, scoped queries and write guards.
- [x] Synthetic demo seed with production refusal; initial PostgreSQL migration.
- [x] Account UI, generated client and offline role/isolation integration tests.
- [ ] Apply migration and seed against PostgreSQL; run browser login/logout for all roles.
- [ ] Verify account layout/keyboard/mobile behavior (browser helper currently fails to initialize).

Milestone 0 does not claim a working expense review, identity system, business MCP catalogue, model provider, or gold-dataset evaluation. Those capabilities enter through the following acceptance gates.

## Delivery sequence

| Milestone | Depends on | Acceptance gate |
| --- | --- | --- |
| 0. Executable skeleton | Contracts and local toolchain | All builds/tests; real health; dependencies start locally; no secrets |
| 1. Identity and tenancy | 0 | Identity cookies, CSRF, four roles, tenant isolation and production demo guard tested |
| 2. Intake and evidence | 1 | Safe image/PDF upload, immutable originals, authorized previews, correction history |
| 3. Policy retrieval | 1–2 | Immutable version/page/clause citations, date selection, deterministic and hybrid retrieval |
| 4. Financial engine | 2–3 | Exact decimal gold cases; explicit currencies, rounding, dated exchange sources |
| 5. MCP boundary | 1–4 | Eight tools/five resources/prompt; SDK discovery snapshot; token/run/tenant authorization; idempotency; Inspector and container smoke |
| 6. Durable agent | 5 | Discovered schemas, fake/live/replay seams, bounded loop, cancellation/recovery, no business bypass, visible sanitized activity |
| 7. Investigation workflow | 2–6 | Conference clarification/resume; valid citations and calculations; validated recommendation |
| 8. Human review | 1, 7 | Separate human decisions; actor/reason/money audit; concurrency; no agent financial authority |
| 9. Evaluations | 5–8 | All release gates in product §18; versioned difficult cases retained; measured dashboard |
| 10. Hardening | 6–9 | End-to-end traces; outbox; safe errors; redaction; bounded recovery; persisted usage agrees |
| 11. Containers/Kubernetes | 10 | Clean Compose/kind; non-root images; Helm/schema checks; probes; migration job; restricted MCP network |
| 12. Terraform/CI | 11 | Validate only; clean-checkout CI; GHCR/SBOM/scanners; disposable kind and Playwright smoke |
| 13. Portfolio | All | README-only clean-checkout demo; full final audit; limitations documented |

## Validation commands

Current commands (PowerShell 7, .NET 10, Node 24, pnpm 11):

```powershell
dotnet restore ExpenseGuard.slnx --locked-mode --configfile NuGet.Config
dotnet build ExpenseGuard.slnx --no-restore
dotnet format ExpenseGuard.slnx --verify-no-changes --no-restore
dotnet test ExpenseGuard.slnx --no-build --no-restore
pnpm --dir web install --frozen-lockfile
pnpm --dir web lint
pnpm --dir web typecheck
pnpm --dir web test
pnpm --dir web build
pwsh -File scripts/check-api-client.ps1
pwsh -File scripts/smoke.ps1
pwsh -File scripts/init-local.ps1
docker compose config --quiet
docker compose up -d --wait
```

Restore/install may download dependencies; test execution must not access external networks, use API keys, or incur charges. Integration tests use ASP.NET TestServer and deterministic probes. Explicit local infrastructure smoke is separate from the offline suite. Future milestones add real Testcontainers, evaluation, Playwright, Helm, kind, Terraform, and security checks before those milestones can pass. Never count an absent tool or skipped check as a pass.
