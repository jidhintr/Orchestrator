---
paths:
  - "tests/**"
  - "k6/**"
---

# Testing rules

- Strategy and levels: `docs/testing/test-strategy.md`. Every scenario is mapped in `docs/testing/coverage-map.md`.
- xUnit v3: use `TestContext.Current.CancellationToken`; run with `dotnet test --solution Orchestrator.slnx`.
- Naming: `Method_Condition_Expected` (CA1707 is disabled for tests only).
- Integration tests use `ApiFactory` (real PostgreSQL via Testcontainers). Do not substitute in-memory providers.
- Test failure paths for every important workflow: invalid input, not found, unauthorized (once auth exists),
  dependency down, duplicates/retries, concurrency, invalid state transitions.
- Deterministic only: no arbitrary sleeps, no shared mutable fixtures, no production services, inject time when it matters.
- A failing test: reproduce, decide whether product or test is wrong, fix the cause. Never delete assertions, skip tests,
  or loosen validation to get green.
- Mark a coverage-map row "Verified" only after the test ran successfully.
- k6: scripts are `k6/<resource>.js`, import `k6/lib/config.js`, support PROFILE smoke/load/spike/soak, and use `check()`
  plus thresholds. Do not invent SLO numbers; thresholds are provisional until the owner defines them.
