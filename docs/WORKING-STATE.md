# Current Working State

## Current objective
Engineering foundation for the Orchestrator API platform. No business feature started.

## Current status
Ready for review (foundation complete locally; CI workflow not yet run on GitHub).

## Relevant modules
See [architecture overview](architecture/overview.md). Only live slice: `HealthController` -> health check `postgres`.

## Decisions and constraints
- [ADR-0001](decisions/ADR-0001-foundation-stack.md): MVC controllers, 4 layers, EF Core/Npgsql, xUnit v3 + Testcontainers, GitHub Actions.
- Host port 5432 is held by an unrelated container (`postgres-prod`); this repo uses 5433. Do not touch it.
- Business domain, auth, UI framework, API versioning, SLOs, deployment target: **undecided, ask the owner.**

## Completed (verified 2026-10-21, local)
- Solution scaffold builds with 0 warnings; `dotnet format --verify-no-changes` clean.
- 6 tests pass (unit + integration against real Postgres, incl. DB-down -> 503).
- API run locally: `/api/health` 200, Scalar at `/scalar/v1` 200, `k6 run k6/health.js` smoke passed.

## Remaining
- Define first business use case(s) in `docs/requirements/use-cases.md`.
- Decide auth, API versioning, SLOs, deployment (each needs an ADR).
- Push to GitHub and confirm `.github/workflows/ci.yml` (unverified), then enable branch protection.
- Run k6 load/spike profiles once real endpoints exist.

## Verification
See Completed. Not run: `.http` file in an IDE client (equivalent `curl` used), CI workflow, k6 load/spike/soak.

## Known issues and risks
- `Application` and `Domain` projects intentionally contain no code yet.
- k6 thresholds (p95 < 500 ms, errors < 1%) are provisional, not SLOs.

## Next action
Owner supplies the first business use case; then follow the `implement-feature` skill.
