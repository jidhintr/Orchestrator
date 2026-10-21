---
paths:
  - "src/**"
  - "docker-compose.yml"
  - ".github/**"
---

# Security rules

- Authentication is **undecided**. Do not invent a real scheme; ask. Until decided, reads are anonymous (say so in docs) and
  changes identify the caller with the temporary, trusted `X-Acting-User-Id` header (ADR-0003). Never use it for anything
  beyond local/dev. Once auth is decided, replace `ActingUser` and keep the 401/403 scenarios.
- Never commit secrets. Connection strings come from user-secrets / environment variables. `.env` is gitignored;
  no `.env` template is committed. No real credentials in tests, docs, `.http` or k6 files.
- Treat all client input as untrusted; validate server-side. Never trust client-supplied identity, role, price or permission (the temporary acting-user header is the one documented exception).
- Use EF Core parameterization; no string-built SQL.
- Do not log secrets, connection strings or personal data.
- Scalar/OpenAPI are Development-only; do not expose them in other environments without an explicit decision.
- Do not disable a security check to make a test pass.
- Do not run destructive commands (drop, truncate, container removal) against shared or non-repo resources.
