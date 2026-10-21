---
name: debug-issue
description: Reproduce, isolate, fix and regression-test a bug or failing test. Use when something behaves incorrectly or a test/CI check fails.
---

# debug-issue

**Purpose:** fix the root cause with a regression test, without side effects.
**Inputs:** symptom, expected behavior, error text or failing test name, environment.

## Workflow
1. Restate expected vs actual. Distinguish product defect, test defect, environment (Docker, port, DB), flaky test.
2. Reproduce with the narrowest command (single test via `dotnet test --project ... --filter-method ...`, one `.http`
   request, or `k6` smoke). Capture only the final error / failing assertion.
3. Isolate: `Grep` the failing symbol, read the relevant method and its callers. Form a hypothesis before editing;
   do not rerun the whole suite hoping it changes.
4. Write a failing regression test first (right level: unit, else integration) and confirm it fails for the right reason.
5. Fix the cause with the smallest change. Do not weaken tests, skip them, or relax validation/security.
6. Run the regression test, then the affected project, then the full suite; run format and build.
7. Update `docs/testing/coverage-map.md`, `.http`/k6 if the contract changed.

## Output
Root cause, fix, regression test name, commands and actual results, remaining risks.

## Stop and ask when
Cannot reproduce, root cause is in an undocumented business rule, the fix changes a public contract or schema, or the
only way to pass is to change expected behavior.
