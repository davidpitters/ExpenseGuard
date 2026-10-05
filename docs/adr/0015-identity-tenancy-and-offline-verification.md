# 0015 — Organization-scoped Identity sessions

Accepted 2026-10-05.

Use ASP.NET Core Identity users with explicit organization memberships and a single organization/role per cookie. A workspace slug selects membership at login; the server validates it and issues the organization claim. Subsequent tenant identity comes exclusively from authenticated claims. Validate membership, role, security stamp, lockout and demo eligibility on every request. Administrator does not imply FinanceReviewer.

Cookies use __Host-, Secure, HttpOnly, SameSite=Strict and a fixed 30-minute lifetime. Antiforgery tokens are issued over HTTPS and required for login/logout. Local Vite terminates TLS and overwrites forwarded protocol; the API trusts only loopback. Production ingress trust and shared protected Data Protection key storage remain deployment-hardening work.

EF filters deny unscoped organization reads. SaveChanges guards reject writes outside the current tenant. IdentityDirectory owns narrowly constrained cross-scope login/session lookups. Global Identity users are never exposed as entities. Bulk SQL/ExecuteUpdate bypassing SaveChanges is not permitted for tenant writes.

Offline integration tests use relational SQLite while Docker is unavailable. This is a verification substitute, not a production provider change. Npgsql remains the runtime provider. The PostgreSQL migration and snapshot are committed; model comparison and SQL generation run offline. Actual PostgreSQL migration application remains unverified.

Official references reviewed: [ASP.NET Core antiforgery](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0) and [EF query filters](https://learn.microsoft.com/en-us/ef/core/querying/filters). Filters complement authorization and write checks; they are not a complete security boundary.
