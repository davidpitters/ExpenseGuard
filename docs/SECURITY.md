# Security design and implementation status

Milestone 1 implements HTTPS-only secure/HttpOnly/Strict cookies, antiforgery on login/logout, five-attempt lockout, fixed 30-minute sessions, per-request membership/role/stamp checks, organization query filters and SaveChanges write guards. See ADR 0015. Demo accounts and initialization flags are rejected outside Development/Test. Account recovery, production key-ring persistence, deployment proxy configuration and broader rate limiting remain future hardening work.

The model is never a security boundary. Financial decisions belong exclusively to authenticated finance reviewers through the human API. Recommendations and decisions must remain separate immutable records. Authorizations are enforced in server code and transaction state, never tool descriptions.

Milestone 0 controls: no expense endpoints, no model calls, locked MCP route, development-only diagnostic routes, allowlisted local Host headers, bounded dependency probes, safe health messages, no checked-in secrets, loopback Compose ports, explicit local credential generation, authored-code warnings as errors.

Milestones 2–10 must add tenant-safe object access, optimistic concurrency, run-scoped JWTs, immutable evidence, upload signatures and malware scanning, append-only audit, rate limits, CSP and retention. This list is a requirement, not a claim that those features already exist.

Tests run with deterministic local fakes and ASP.NET in-memory TestServer. Dependency restore is separate from test execution. No automated test may send model requests or require a key. Explicit developer infrastructure smoke may contact loopback services.

Local `.env`, build logs, data, test output and credentials are ignored. Production must not use local demo setup. Do not enable external MCP access or provision resources without explicit authorization.
