# Threat model

Identity verification now tests forged/missing CSRF, wrong-workspace login, cross-organization identifiers, revoked membership/role sessions, password lockout, unscoped data access and production reuse of demo data. Deployment threats (shared Data Protection keys, ingress trust, rate limiting) remain open until hardening. Browser verification and real PostgreSQL startup are still outstanding; see STATUS.md.

Trust boundaries: browser → API; uploaded evidence → extraction; worker/model → MCP; MCP → tenant data; recommendation → human decision. Only validated identity, server claims, authorization policies and deterministic rules are authoritative.

| Threat | Required control | Verification milestone |
| --- | --- | --- |
| Receipt/policy prompt injection | Typed untrusted fields; bounded tools; server validation; no financial tools | 5, 7, 9 |
| Cross-tenant identifiers | Claims-based scope, tenant filters, object checks, indistinguishable not-found | 1, 5, 9 |
| Stolen or replayed run token | Short TTL, audience/signature, run/expense/org/scope binding, state checks | 5 |
| Replayed action | Transactional idempotency key plus normalized request hash | 5–6 |
| Model arithmetic errors | Authoritative decimal calculator, persisted calculation reference | 4, 7 |
| Recommendation changes human decision | Separate API and records, authorization and concurrency | 8–9 |
| Malicious file/path/URL | Signature/size/type checks, scan seam, opaque object keys, no arbitrary fetch tool | 2, 5 |
| Denial of service or excessive spend | Bounded files/results/queries/time/turns/tokens/tools/cost and cancellation | 2, 5–6, 10 |
| Telemetry leaks | No evidence bodies, tokens, credentials or private reasoning; redact metadata | 10 |
| Local credential reuse in production | Explicit development-only seed/auth guard; no defaults in deploy secrets | 1, 11 |
| Host rebinding / exposed diagnostics | Local allowed hosts and development-only routes | 0, 10 |

Residual risk at milestone 0: no authenticated product exists. Health endpoints expose only generic health; local diagnostics expose component status but no connection strings. The locked MCP route is temporary and must not be replaced with permissive demo authentication.
