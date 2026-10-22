---
name: feature-checklist
description: Checklist for implementing, fixing or reviewing a feature end to end (use case, code, docs, .http, k6, scenarios, verification). Use for any non-trivial feature, bug fix or pre-merge review.
---

# feature-checklist

0. Invoke `ponytail:ponytail` before touching code and follow it throughout (mandatory, see CLAUDE.md).
1. `git status --short`. Ask about anything unclear (actor, rules, permissions, acceptance criteria); never guess.
2. Add or confirm the use case in `docs/requirements.md` (flows, failures, open questions).
3. Search for the affected code first. Check impact: public contract, schema/migration, auth, shared code. Ask before
   breaking changes, destructive migrations or new dependencies.
4. Implement smallest-first: Domain -> Application -> Infrastructure (+ migration) -> Api.
5. Update the four API artifacts (`.claude/rules/api-artifacts.md`).
6. Bug fix: write the failing scenario first, confirm it fails for the right reason, then fix the cause.
7. Verify: format, build, `dotnet test`, run the API and exercise the `.http` requests, `k6` smoke, EF drift check.
8. Review `git diff --stat` and the relevant diffs: only intended files, no secrets, docs updated.
9. Report: what changed, commands run with real results, unverified items, risks, decisions needed.

Stop and ask when requirements are ambiguous, a change is breaking or destructive, auth behavior is needed (undecided),
or a scenario can only pass by changing expected behavior.
