# Orchestrator

.NET 10 API for tracking the assembly line of a printing press, backed by PostgreSQL. A UI (Angular or Node.js,
**undecided**) will join this solution later. Requirements: [docs/requirements.md](docs/requirements.md).
Decisions: [docs/decisions.md](docs/decisions.md). Instructions for Claude Code: [CLAUDE.md](CLAUDE.md).

**Stack:** ASP.NET Core MVC controllers, EF Core + Npgsql, PostgreSQL 17, OpenAPI + Scalar, Reqnroll (Gherkin) on
xUnit v3 + Testcontainers, k6, `.http` files, GitHub Actions.
**Prerequisites:** .NET SDK 10.0.x (`global.json`), Docker, [k6](https://k6.io), `dotnet tool install -g dotnet-ef`.

## Run
```bash
echo ORCHESTRATOR_DB_PASSWORD=<password> > .env   # gitignored; port defaults to 5433
docker compose up -d --wait postgres
dotnet user-secrets set "ConnectionStrings:Orchestrator" "Host=localhost;Port=5433;Database=orchestrator;Username=orchestrator;Password=<.env password>" --project src/Orchestrator.Api
dotnet ef database update --project src/Orchestrator.Infrastructure --startup-project src/Orchestrator.Api
dotnet run --project src/Orchestrator.Api --launch-profile http
```
API http://localhost:5176, docs (Scalar, Development only) http://localhost:5176/scalar/v1.
Requests: open `http/*.http` and pick the `local` environment. Migrations are applied at startup only when `MigrateOnStartup=true` (set for the `api` compose service, which uses database `OrchStore`, schema `biz`, user `sa`, password `ORCHESTRATOR_HOST_DB_PASSWORD` in `.env`).

## Check
```bash
dotnet format Orchestrator.slnx --verify-no-changes
dotnet build Orchestrator.slnx
dotnet test --solution Orchestrator.slnx    # Docker must be running; starts a throwaway Postgres
k6 run k6/departments.js                    # API running; -e PROFILE=load|spike|soak (creates rows), -e BASE_URL=...
```
CI (`.github/workflows/ci.yml`): format, build, tests, EF drift check, vulnerable packages, k6 smoke.

## Layout
| Path | Purpose |
|---|---|
| `src/Orchestrator.Api` | Controllers, a service and DTOs per resource, middleware, error handling, versioning, OpenAPI/Scalar |
| `src/Orchestrator.Infrastructure` | Entities, `DbContext`, EF configuration, seed, migrations |
| `tests/Orchestrator.Specs` | Gherkin `Features/`, `StepDefinitions/`, `Support/ApiHost` (API + Postgres container) |
| `http/`, `k6/` | Request files and load scripts, kept in sync with endpoints |

## Troubleshooting
- `port is already allocated`: change `ORCHESTRATOR_DB_PORT` in `.env`.
- Tests cannot start: Docker must be running.
- Use `dotnet test --solution ...`; xUnit v3 on .NET 10 runs on Microsoft.Testing.Platform.
