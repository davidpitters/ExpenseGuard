# 0013 — Pin stable SDKs and compatible frontend tools

Status: accepted for implementation · 2026-10-02

Registry metadata checked 2026-10-02: official MCP C# 2.2.0, OpenAI .NET 2.14.0, EF/ASP.NET 10.0.12, Npgsql EF 10.0.3. MCP uses stateless Streamable HTTP and disables automatic SSE fallback in the client. Persist negotiated revisions with runs in milestone 6. React 19.3.0 / Vite 8.3.2 run on Node 24; pnpm 11.25.0 is available locally. TypeScript stays at stable 5.9.3 because openapi-typescript 7.13.0 declares TypeScript ^5.x, while typescript-eslint 8.71.0 requires <6.1.0. Latest TypeScript 7.0.2 would violate those peer contracts. Package lockfiles are checked in. ServiceDefaults adds one host-plumbing project to the contracted structure. No preview MCP package is used.

Reference: https://raw.githubusercontent.com/modelcontextprotocol/csharp-sdk/v2.2.0/docs/concepts/transports/transports.md
