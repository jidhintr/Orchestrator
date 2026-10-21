---
name: implement-feature
description: Implement a new feature or endpoint end to end (use case, controller, persistence, Scalar docs, .http, k6, tests, docs). Use when the user asks to add or extend behavior.
---

# implement-feature

**Purpose:** deliver the smallest complete, verified change for a requirement.
**Inputs:** the requirement. If actor, rules, inputs/outputs, permissions or acceptance criteria are unclear, ask. Never guess.

## Workflow
1. `git status --short`; confirm branch and no unrelated changes.
2. Ensure a use case exists in `docs/requirements/use-cases.md` (ID, flows, failure cases, authz, data effects,
   acceptance criteria). Add it first if missing; get user confirmation for non-trivial rules.
3. Inspect only the affected area: `Grep` for related controllers, contracts, DbContext config, tests.
4. Assess impact: public contract, schema/migration, auth, shared code. Ask before breaking changes or new dependencies.
5. Short checklist plan; implement in order Domain -> Application -> Infrastructure (+ migration) -> Api controller.
6. Update the four artifacts per `.claude/rules/api-artifacts.md`: Scalar annotations, `http/*.http`, `k6/*.js`,
   tests + coverage-map.
7. Verify: `dotnet format --verify-no-changes`, `dotnet build`, targeted tests then full `dotnet test`, run the API
   and exercise the `.http` request, `k6 run` smoke.
8. Review `git diff --stat` and relevant diffs; update affected docs (architecture, ADR if significant).

## Output
What changed and why, key files, commands run with actual results, unverified items, risks.

## Stop and ask when
Requirements are ambiguous, a schema change is destructive, a new dependency/abstraction seems needed, auth behavior is
required (undecided), or a test cannot be made to pass without changing expected behavior.
