---
name: testing
description: Find coverage gaps against use cases, add missing tests, and run the right unit, integration and k6 suites. Use for test planning, coverage review or running verification.
---

# testing

**Purpose:** prove behavior against use cases, not just code paths.
**Inputs:** use case ID(s) or the changed area.

## Workflow
1. Read the use cases and `docs/testing/coverage-map.md` rows for the scope only.
2. List missing scenarios: happy path, invalid input, not found, duplicates/retries, dependency failure, concurrency,
   authz (once decided), boundary values.
3. Add tests at the lowest effective level (`.claude/rules/testing.md`); integration tests use `ApiFactory`.
4. Run, narrow to broad:
   - `dotnet test --project tests/Orchestrator.UnitTests`
   - `dotnet test --project tests/Orchestrator.IntegrationTests` (Docker must be running)
   - `dotnet test --solution Orchestrator.slnx`
   - `k6 run k6/<file>.js` (smoke; `-e PROFILE=load|spike|soak` only when requested or relevant)
5. Update coverage-map: "Verified" only for tests that just passed; otherwise "Pending" or "Unverified".

## Output
Gaps found, tests added, commands with actual pass/fail counts, unverified items and why.

## Stop and ask when
Expected behavior is undefined, thresholds/SLOs are needed for load tests (never invent them), Docker or the API is
unavailable (report as unverified, do not treat as passed), or a test is flaky (diagnose, do not retry blindly).
