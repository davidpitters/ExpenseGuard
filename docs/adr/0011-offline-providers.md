# 0011 — Require fake and replay providers

Status: accepted for implementation · 2026-10-02

Tests must execute offline without paid services or credentials. Deterministic fakes exercise the workflow; recordings enable regression replay and need redaction/version provenance. Initial host integration tests use TestServer and deterministic dependency probes. Local infrastructure smoke is explicitly separate.
