# ExpenseGuard status

Updated: 2026-10-05 (America/Toronto).

## Current milestone

Milestone 1 — identity and tenancy implemented and verified offline. Milestone 0 dependency startup and milestone 1 PostgreSQL/browser acceptance remain open. Portfolio release is not complete.

## Completed

- Read both implementation contracts completely; original briefs preserved.
- Inspected workspace: only three supplied Markdown files; no Git repository or existing application.
- Verified .NET SDK 10.0.401 / runtime 10.0.12 and bundled Node 24.19.0.
- Queried official NuGet stable package metadata and checked official framework/MCP/API documentation.
- Created the operational plan and design/security/evaluation documentation before feature implementation.

## Decisions

- Identity uses organization memberships, secure cookies and per-request role/stamp checks. ADR 0015 records HTTPS/proxy scope and the SQLite verification substitute.
- Account UI and a bounded admin/auditor membership directory are implemented. Financial decision and role-administration mutation endpoints remain absent.

- Follow the numbered milestones. Build one C# agent with a separate MCP service.
- Business MCP discovery remains empty and its route denies every caller in milestone 0. Run-scoped JWT authorization and business tools arrive together in milestone 5.
- No live model calls, cloud work, deployments, or real financial data.
- Diagnostic UI reports real connectivity. Missing dependencies cannot be represented as healthy.

## Validation results

Verified on 2026-10-02 before the interrupted final documentation pass:

- Backend locked restore succeeded; build succeeded with 0 warnings and 0 errors.
- Backend tests: 46 passed, 0 failed, 0 skipped (10 unit, 11 agent-budget, 6 architecture, 12 MCP boundary, 7 API integration).
- `dotnet format --verify-no-changes --no-restore` passed.
- Frontend frozen install passed with pnpm supply-chain checks enabled (349 entries). Offline install alone lacked cached registry metadata; this is not a test-execution dependency.
- Frontend lint, strict typecheck, production build and formatting passed; 7 component tests passed.
- OpenAPI/client freshness check passed.
- Three-host local smoke passed: liveness 200 on all hosts, readiness 503 with missing dependencies, MCP 401, web shell and API proxy 200.
- Browser checks: desktop and 390px mobile rendering, navigation and refresh preferences. Screenshot saved under ignored `artifacts/screenshots/workspace.jpg`.
- Compose/container startup was NOT verified. No product evaluations or full expense-workflow tests exist yet.

On 2026-10-05 the normal command sandbox failed to initialize. Approved execution outside that failing helper can inspect the workspace. Existing foundation is committed as `6824b8e`; it is being preserved.

Verified on 2026-10-05:

- Backend build: 0 warnings, 0 errors. Offline tests: 63 passed, 0 failed, 0 skipped (24 API integration, 39 existing tests).
- Integration tests cover four roles, wrong-workspace/cross-tenant denial, missing/forged CSRF, cookie flags, logout, lockout, membership/role revocation, unscoped read/write denial, idempotent seed, Production demo rejection and initialization refusal.
- Frontend typecheck, lint and production build passed. 10 component tests passed, including token-protected login/logout, HTTPS failure and unavailable session handling.
- OpenAPI 3.1/TypeScript contracts updated. EF reports no model changes since the initial migration. PostgreSQL SQL generation succeeded into ignored `.local/identity-migration.sql`; no PostgreSQL migration was applied.
- Final locked restore, backend formatting verification, frontend formatting and OpenAPI/client freshness passed. Diff whitespace check passed. Identity tests passed again after stable seed IDs were added.
- A failing startup test led to a guard on finalized host configuration before development migration/seed can run in Production.
- Browser automation failed before UI access: `windows sandbox failed: helper_unknown_error: setup refresh had errors`. No new-screen visual/keyboard/browser authentication verification claimed.

## External limitations

- Docker, Helm, kind, and Terraform are not on PATH. No Docker installation found in the standard Program Files location. Container startup cannot currently be exercised.
- Default Node resolves to an incompatible older runtime. Validation uses bundled Node 24.19.0 and pnpm 11.25.0 explicitly on PATH.
- Package registry access requires the environment's network permission. Official NuGet metadata query succeeded using approved network access.
- This workspace was not initially a Git checkout; clean-checkout final audit is a later release gate.

## Next work

Run outstanding infrastructure/browser gates with docs/LOCAL_SETUP.md when local tools are available. Then continue milestone 2 intake and immutable evidence storage. SQLite tests do not substitute for PostgreSQL startup. No live model, product evaluations, full expense workflow or Kubernetes validation has run.
