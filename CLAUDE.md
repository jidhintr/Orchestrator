# Orchestrator - Claude Code instructions

.NET 10 API platform on PostgreSQL (EF Core), MVC controllers, Scalar docs. UI framework (Angular/Node) is **undecided**.
Overview: [README.md](README.md). Architecture: [docs/architecture/overview.md](docs/architecture/overview.md).

## Never guess
If a requirement, business rule, name, SLO, auth choice or technology is not in the repo or the user's words, **ask**.
Do not invent use cases, thresholds or architecture. Record answers in `docs/` (ADR for significant choices).

## Session start
1. `git status --short`, `git branch --show-current`.
2. Read [docs/WORKING-STATE.md](docs/WORKING-STATE.md) only if continuing prior work; verify it against the repo.
3. Read only the architecture/use-case docs the task touches. Search (`Grep`/`Glob`) before opening files.

## Commands
| Task | Command |
|---|---|
| Start DB | `docker compose up -d --wait` (needs `.env`, see [local dev](docs/operations/local-development.md)) |
| Run API | `dotnet run --project src/Orchestrator.Api --launch-profile http` (http://localhost:5176) |
| Format check | `dotnet format Orchestrator.slnx --verify-no-changes` (fix: drop the flag) |
| Build (warnings = errors) | `dotnet build Orchestrator.slnx` |
| Tests | `dotnet test --solution Orchestrator.slnx` (integration needs Docker) |
| Single project | `dotnet test --project tests/Orchestrator.UnitTests` |
| k6 | `k6 run k6/<file>.js` (`-e PROFILE=smoke\|load\|spike\|soak`, `-e BASE_URL=...`) |

## Structure
`src/Orchestrator.{Api,Application,Domain,Infrastructure}`, `tests/Orchestrator.{UnitTests,IntegrationTests}`,
`http/` (request files), `k6/` (load scripts), `docs/`. Dependencies point inward (Api -> Application -> Domain;
Infrastructure -> Application). Controllers are thin; business rules go in Application/Domain.

## Mandatory for every endpoint (details in `.claude/rules/api-artifacts.md`)
Controller + XML docs + `[ProducesResponseType]` (feeds Scalar) -> `http/<resource>.http` entry -> `k6/<resource>.js`
coverage -> unit/integration tests -> row in [coverage-map](docs/testing/coverage-map.md). An endpoint change without
the `.http` and k6 update is incomplete.

## Rules (auto-scoped by path in `.claude/rules/`)
`coding-standards.md`, `testing.md`, `security.md`, `api-artifacts.md`. Skills: `implement-feature`, `debug-issue`,
`testing`, `release-review`.

## Token efficiency
Targeted reads, `git diff --stat`, filtered test output (failing test + final error). Do not read `bin/`, `obj/`,
lockfiles or generated files. Reuse existing code before adding abstractions or dependencies. Never skip needed
investigation or tests merely to save tokens.

## Verification and honesty
Run format, build and relevant tests before declaring done; review `git diff`. Never claim a check passed unless it ran
successfully; mark anything not run as **unverified**. Never weaken a test, assertion or security check to get green.

## Keeping context current
Update only affected docs: use-cases, coverage-map, architecture, ADRs, `.http`, k6. For multi-session work update
[docs/WORKING-STATE.md](docs/WORKING-STATE.md) (concise, no transcript, no secrets). Do not commit or push unless asked.

## Environment notes
Connection string comes from user-secrets or `ConnectionStrings__Orchestrator`; never commit it. Host port 5432 may be
occupied by an unrelated container; this repo defaults to 5433. Do not touch containers this repo did not create.
