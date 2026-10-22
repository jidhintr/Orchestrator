---
paths:
  - "src/**"
  - "docker-compose.yml"
  - ".github/**"
---

# Security rules

- Authentication is password sign-in with JWT (ADR-0005). Every endpoint is behind the fallback authorization policy; `[AllowAnonymous]`
  only for health, sign in, refresh and logout. Never log or return passwords, tokens or hashes. Dev-phase gaps: `docs/TODO-auth-end-of-dev.md`.
- Never commit secrets. Connection strings come from user-secrets / environment variables. `.env` is gitignored;
  no `.env` template is committed. No real credentials in tests, docs, `.http` or k6 files.
- Treat all client input as untrusted; validate server-side. Never trust client-supplied identity, role, price or permission.
- Use EF Core parameterization; no string-built SQL.
- Do not log secrets, connection strings or personal data.
- Scalar/OpenAPI are Development-only; do not expose them in other environments without an explicit decision.
- Do not disable a security check to make a test pass.
- Do not run destructive commands (drop, truncate, container removal) against shared or non-repo resources.
