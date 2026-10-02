# Upstream's security-fixed release is source-only. See ADR 0014.
FROM golang:1.26.8-bookworm AS build
ENV CGO_ENABLED=0 GOBIN=/out
RUN go install github.com/minio/minio@RELEASE.2025-10-15T17-29-55Z \
    && find /go/pkg/mod/github.com/minio -maxdepth 2 -path '*/minio@*/LICENSE' -exec cp {} /out/LICENSE \;

FROM debian:bookworm-slim
RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates curl \
    && rm -rf /var/lib/apt/lists/* \
    && useradd --uid 10001 --create-home minio \
    && mkdir -p /data \
    && chown 10001:10001 /data
COPY --from=build /out/minio /usr/local/bin/minio
COPY --from=build /out/LICENSE /usr/share/doc/minio/LICENSE
USER 10001:10001
EXPOSE 9000 9001
ENTRYPOINT ["minio"]
