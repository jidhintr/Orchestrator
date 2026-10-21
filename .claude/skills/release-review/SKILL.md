---
name: release-review
description: Review the final diff for regression risk, security, contract compatibility, documentation and release readiness. Use before merge, commit or release.
---

# release-review

**Purpose:** independent final check against the Definition of Done.
**Inputs:** branch or diff range (default: working tree vs `master`).

## Workflow
1. `git status --short`, `git diff --check`, `git diff --stat`; read only relevant diffs in full.
2. Scope: only intended files; no secrets, `.env`, debug code, generated artifacts, unrelated refactors.
3. Contracts: API shape, status codes, DB schema/migrations (backward compatible? rollback?), config keys.
4. Security: validation at boundaries, authz (undecided: note it), secrets, logging of sensitive data, injection.
5. Artifacts in sync (`.claude/rules/api-artifacts.md`): Scalar annotations, `http/`, `k6/`, tests, coverage-map.
6. Run: `dotnet format Orchestrator.slnx --verify-no-changes`, `dotnet build`, `dotnet test --solution`,
   `dotnet list package --vulnerable --include-transitive`, k6 smoke. CI config changes cannot be proven locally; say so.
7. Docs reflect reality (README, architecture, ADRs, WORKING-STATE).

## Output
Verdict (ready / not ready), findings ordered by severity with file references, commands run with actual results,
unverified items.

## Stop and ask when
A breaking change or destructive migration is present, a security-sensitive change lacks negative tests, or required
checks cannot run. Do not call anything production-ready on a green build alone.
