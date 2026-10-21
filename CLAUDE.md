# Orchestrator - Claude Code instructions

.NET 10 API for a printing-press assembly-line tracker, PostgreSQL + EF Core, MVC controllers, Scalar docs.
UI: React + Vite PWA in `ui/` (ADR-0004). Setup and commands: [README.md](README.md).
Requirements and open questions: [docs/requirements.md](docs/requirements.md). Decisions: [docs/decisions.md](docs/decisions.md).
Continuing prior work: [docs/WORKING-STATE.md](docs/WORKING-STATE.md) (verify against the repo first).

## MUST: use the ponytail skill for every code change
Before writing or editing any code (source, tests, Gherkin, migrations, scripts, CI), invoke the Skill tool with
`ponytail:ponytail` if it is not already active in this session, and follow it for the whole task: smallest complete
change, no bloat, no speculative abstractions, short replies ending with what was skipped or unchecked.
The repo rules below still apply on top of it (never guess, the endpoint checklist, verification, honest reporting).
If the skill cannot be invoked, say so before writing code instead of silently skipping it.

## Never guess
If a requirement, business rule, name, SLO, auth choice or technology is not in the repo or the user's words, **ask**.
Do not invent use cases, thresholds or architecture. Record answers in `docs/requirements.md`; significant choices in `docs/decisions.md`.

## Structure
`src/Orchestrator.Api` (controllers, a service + DTOs per resource, errors, middleware) -> `src/Orchestrator.Infrastructure`
(entities, `DbContext`, EF config, seed, migrations); `tests/Orchestrator.Specs`; `http/`; `k6/`.
Detailed code rules: `.claude/rules/coding-standards.md` (loads for `src/**/*.cs`).
Keep classes small; no placeholder files, new packages or abstractions without a concrete need.
Data model: every entity inherits `BaseEntity` (int identity PK from 1000, audit columns, `IsActive`); relate tables by PK/FK only. Details in `.claude/rules/coding-standards.md`.

## Every endpoint change (scoped rule: `.claude/rules/api-artifacts.md`)
Controller + `[EndpointSummary]` + `[ProducesResponseType]` for every status (feeds Scalar) -> `http/<resource>.http` -> `k6/<resource>.js` ->
Gherkin scenarios tagged `@UC-xxx`. Incomplete if any is missing. Use `/feature-checklist` for a full feature.

## Testing (owner decision: BDD only for now)
Reqnroll + Gherkin on xUnit v3 in `tests/Orchestrator.Specs`; API hosted in-process against a Testcontainers Postgres
with real migrations. No unit/integration/e2e projects until the owner asks. Generic steps in `ApiSteps.cs`;
placeholders `{unique}` and `{id}`. Scenarios come from the owner or documented use cases, never invented.
Cover failure paths. Never weaken, skip or delete a scenario to get green.

## Security
Auth is undecided: reads are anonymous and say so; changes use the temporary `X-Acting-User-Id` header (ADR-0003), which is trusted, not secure, and replaced when auth lands. No secrets in git (`.env` ignored; connection string via
user-secrets or `ConnectionStrings__Orchestrator`). Validate server-side, no string-built SQL, no exception text in 500
bodies, Scalar/OpenAPI Development-only. The repo creates no Postgres container: the API uses the existing one on host port 5432 (database `OrchStore`, schema `biz`); don't modify or remove that container.

## Commands
| Task | Command |
|---|---|
| Format check | `dotnet format Orchestrator.slnx --verify-no-changes` |
| Build (warnings = errors) | `dotnet build Orchestrator.slnx` |
| All tests (needs Docker) | `dotnet test --solution Orchestrator.slnx` |
| One use case | `dotnet test --project tests/Orchestrator.Specs --filter-trait "Category=UC-001"` |
| New migration | `dotnet ef migrations add <Name> --project src/Orchestrator.Infrastructure --startup-project src/Orchestrator.Api --output-dir Persistence/Migrations` |
| Apply / drift check | same projects: `dotnet ef database update` / `dotnet ef migrations has-pending-model-changes` |
| k6 | `k6 run k6/<file>.js` (`-e PROFILE=smoke\|load\|spike\|soak`) |

## Working rules
- Search before reading; read targeted sections; skip `bin/`, `obj/`, generated files, `*.feature.cs`.
- Before finishing: format, build, relevant tests, `git diff`. Report only what actually ran; mark the rest **unverified**.
- Update only affected docs. Keep each fact in one place, preferably in code or tests. Don't commit or push unless asked.
- Never edit an applied migration; no destructive migration without approval.
- **No comments by default.** Never write XML doc comments (`/// <summary>`, `<param>`, `<response>`), function descriptions,
  banner/section comments, or comments restating the code. Not in controllers, DTOs, services, migrations, tests, step
  definitions, Gherkin descriptions, `.http`, k6 or CI files. Add one short `//` only for a non-obvious *why* that is
  crucial (a hidden constraint, a workaround, a deliberate shortcut). Names and tests carry the meaning.
