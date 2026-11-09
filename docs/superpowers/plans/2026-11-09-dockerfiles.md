# Dockerfile improvements implementation plan

**Goal:** Improve server and agent source/release images while preserving their existing entrypoints and verified-release packaging.

**Architecture:** Keep .NET 10 multi-stage source builds and runtime-only release images. Restore before copying source, publish once with --no-restore, and share a BuildKit NuGet cache between restore and publish. Align server writable paths, HTTP health checks and non-root runtime settings across both image variants.

**Constraints:** Preserve Visual Studio base/final stages, release app/ contexts, original encoding and CRLF. Do not commit existing workspace changes or publish images.

- [ ] Update all four Dockerfiles, the server DockerfileContext and root build-context exclusions.
- [ ] Check Docker build syntax and source builds; report existing dependency failures without changing unrelated package versions.
- [ ] Build release images from existing local application payloads; verify non-root ownership, writable paths, server HTTP health and agent entrypoint.
- [ ] Review diff and report results and limitations.
