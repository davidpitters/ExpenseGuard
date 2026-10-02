# 0008 — Deploy MCP separately

Status: accepted for implementation · 2026-10-02

A separate process owns protocol discovery, scoped authorization, bounds and telemetry. Shared application interfaces avoid duplicated business logic. Worker business operations must traverse the official C# client. Architecture tests forbid direct Worker/Agent references to application/infrastructure business services.

Reference: https://github.com/modelcontextprotocol/csharp-sdk
