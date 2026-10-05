# Local development

Current implementation: executable foundation and organization-scoped account access. Expense intake, agent investigation, financial decisions and product evaluations are not implemented yet. No OpenAI key is needed or used.

Prerequisites: PowerShell 7, .NET SDK 10.0.401, Node 24, pnpm 11.25.0, Docker with Compose. Run from the repository root. MinIO builds from pinned official source; initial startup requires network access.

```powershell
pwsh -File scripts/prepare-https.ps1
dotnet dev-certs https --trust
pwsh -File scripts/start-local.ps1 -SeedDemo
```

The trust command changes your local certificate trust store. Exported private material stays in ignored `.local/`; never commit it. Startup creates random infrastructure passwords in ignored `.env`, starts PostgreSQL/pgvector, Redis and MinIO, restores packages, builds, applies the Identity migration and seeds synthetic accounts. Open `https://localhost:5173/access`. For existing infrastructure with `-SkipDependencies`, supply connection settings yourself. The API refuses initialization flags outside Development/Test.

Workspace: `northstar-labs`. All four synthetic accounts use the public demo password `Northstar-Demo-2026!`:

| Email | Role |
| --- | --- |
| employee@northstar.example | Employee |
| finance@northstar.example | FinanceReviewer |
| admin@northstar.example | Administrator |
| auditor@northstar.example | Auditor |

All roles can inspect their session. Administrator and Auditor can view the paginated membership directory. No role/decision mutation endpoint exists yet. Demo accounts cannot authenticate in Production, even if its database was copied from Development. Seeding is idempotent and does not reset passwords or roles.

Without Docker, `pwsh -File scripts/start-local.ps1 -SkipDependencies` starts diagnostics. Readiness reports missing dependencies; sign-in requires PostgreSQL. Without exported certificates the web host uses HTTP and authentication requests return 426. Do not bypass certificate validation.

```powershell
dotnet restore ExpenseGuard.slnx --locked-mode --configfile NuGet.Config
dotnet build ExpenseGuard.slnx --no-restore
dotnet test ExpenseGuard.slnx --no-build --no-restore
dotnet format ExpenseGuard.slnx --verify-no-changes --no-restore
pnpm --dir web install --frozen-lockfile
pnpm --dir web typecheck
pnpm --dir web lint
pnpm --dir web test
pnpm --dir web build
pnpm --dir web format:check
pwsh -File scripts/check-api-client.ps1
dotnet tool restore
dotnet ef migrations has-pending-model-changes --project src/ExpenseGuard.Infrastructure
pwsh -File scripts/smoke.ps1 -WebUrl https://localhost:5173 -RequireDependencies
```

Restore/install uses registries; tests run offline with TestServer, SQLite and deterministic probes. SQLite tests do not prove PostgreSQL migrations executed. Actual Compose startup and browser sign-in remain acceptance gates in STATUS.md.

Ctrl+C stops script-owned application processes. `docker compose down` stops dependencies without deleting volumes. Logs are in ignored `.local/logs`. API/worker/MCP liveness ports are 5100/5101/5102. `/mcp` remains locked with 401; business tools are milestone 5.
