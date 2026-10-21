# Test Strategy

| Level | Project / tool | Scope | Runs |
|---|---|---|---|
| Unit | `tests/Orchestrator.UnitTests` (xUnit v3) | Controller mapping logic, Application/Domain rules; no I/O | every build |
| Integration | `tests/Orchestrator.IntegrationTests` (xUnit v3, `WebApplicationFactory`, Testcontainers PostgreSQL 17) | HTTP -> controller -> EF Core -> real Postgres, including failure paths | every build (needs Docker) |
| Load / spike / soak | `k6/*.js`, profile via `-e PROFILE=` | Per endpoint group; thresholds in `k6/lib/config.js` | smoke in CI; others on demand |
| Manual / exploratory | `http/*.http` | One request set per controller, expected responses in comments | during development |
| End-to-end | not yet | Playwright or framework-native, chosen after the UI framework is decided | future |

## Rules
- Every new endpoint ships with: controller + XML docs, `.http` entry, k6 coverage, unit/integration tests, and a row in
  [coverage-map](coverage-map.md).
- Integration tests use real PostgreSQL via Testcontainers, not in-memory providers.
- Never loosen a test to get green; see `.claude/rules/testing.md`.

## Quality gates (CI)
format check -> build (warnings as errors) -> tests -> vulnerable-package scan -> k6 smoke.

## k6 thresholds
Provisional defaults (p95 < 500 ms, errors < 1%). No SLOs are defined yet. Confirm with the owner before treating
them as targets.
