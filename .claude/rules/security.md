---
paths:
  - "src/**"
  - "docker-compose.yml"
  - ".github/**"
---

# Security rules

- Authentication/authorization is **undecided**. Do not invent a scheme; ask. Until decided, every endpoint is
  anonymous and this must be stated in its docs. Once decided, add negative tests (401/403) per endpoint.
- Never commit secrets. Connection strings come from user-secrets / environment variables. `.env` is gitignored;
  no `.env` template is committed. No real credentials in tests, docs, `.http` or k6 files.
- Treat all client input as untrusted; validate server-side. Never trust client-supplied identity, role, price or permission.
- Use EF Core parameterization; no string-built SQL.
- Do not log secrets, connection strings or personal data.
- Scalar/OpenAPI are Development-only; do not expose them in other environments without an explicit decision.
- Do not disable a security check to make a test pass.
- Do not run destructive commands (drop, truncate, container removal) against shared or non-repo resources.
