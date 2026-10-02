# 0014 — Build the patched local MinIO release from source

Status: accepted for implementation · 2026-10-02

The contract names MinIO for development. Upstream's security release RELEASE.2025-10-15T17-29-55Z directs container users to build source. The local Compose build uses that exact release with Go 1.26.8 instead of an older prebuilt image. This is a dependency build, not a Go application/orchestration service. It is development-only, with generated credentials and loopback ports. Container build and health remain unverified until Docker is available. The S3 seam and Azure Blob production adapter remain future work; review MinIO maintenance before any production use.

Reference: https://github.com/minio/minio/releases/tag/RELEASE.2025-10-15T17-29-55Z
