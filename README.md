# Orchestrator

.NET 10 API for tracking the assembly line of a printing press, backed by PostgreSQL. A React UI lives in [ui/](ui) (see UI below). Requirements: [docs/requirements.md](docs/requirements.md).
Decisions: [docs/decisions.md](docs/decisions.md). Instructions for Claude Code: [CLAUDE.md](CLAUDE.md).

**Stack:** ASP.NET Core MVC controllers, EF Core + Npgsql, PostgreSQL 17, OpenAPI + Scalar, Reqnroll (Gherkin) on
xUnit v3 + Testcontainers, k6, `.http` files, GitHub Actions.
**Prerequisites:** .NET SDK 10.0.x (`global.json`), Docker, [k6](https://k6.io), `dotnet tool install -g dotnet-ef`.

## Run
```bash
echo ORCHESTRATOR_HOST_DB_PASSWORD=<sa password> > .env   # gitignored
docker compose up -d --build            # API on http://localhost:8080, needs your Postgres container on host port 5432
dotnet user-secrets set "ConnectionStrings:Orchestrator" "Host=localhost;Port=5432;Database=OrchStore;Username=sa;Password=<sa password>" --project src/Orchestrator.Api
dotnet ef database update --project src/Orchestrator.Infrastructure --startup-project src/Orchestrator.Api
dotnet run --project src/Orchestrator.Api --launch-profile http
```
Tables live in schema `biz`. The compose `api` service migrates at startup (`MigrateOnStartup=true`); a plain `dotnet run` does not.
API http://localhost:5176, docs (Scalar, Development only) http://localhost:5176/scalar/v1.
Requests: open `http/*.http` and pick the `local` environment.

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
| `src/Orchestrator.Domain` | Entities, `BaseEntity`, `SystemIds`, `Rank` (no dependencies) |
| `src/Orchestrator.Application` | A service and DTOs per resource, `IAppDbContext`, acting-user rules, exceptions |
| `src/Orchestrator.Infrastructure` | `DbContext`, EF configuration, seed, migrations, Postgres wiring |
| `src/Orchestrator.Api` | Controllers, middleware, error handling, versioning, OpenAPI/Scalar |
| `tests/Orchestrator.Specs` | Gherkin `Features/`, `StepDefinitions/`, `Support/ApiHost` (API + Postgres container) |
| `tests/Orchestrator.UnitTests` | Domain, services (EF InMemory), DTO validation, error handler, middleware |
| `tests/Orchestrator.IntegrationTests` | Every endpoint over HTTP against a Testcontainers Postgres |
| `http/`, `k6/` | Request files and load scripts, kept in sync with endpoints |

## Troubleshooting
- `port is already allocated`: change `ORCHESTRATOR_API_PORT` in `.env`.
- Tests cannot start: Docker must be running.
- Use `dotnet test --solution ...`; xUnit v3 on .NET 10 runs on Microsoft.Testing.Platform.

## UI

`ui/` is a React + TypeScript PWA (Vite). Start the API (port 5176), then `cd ui; npm install; npm run dev`.
Vite proxies `/api` to the API, so no CORS setup is needed. Screens: Departments, Users, Roles, Clients, Catalogs and Approvals (left nav on desktop, bottom tabs on phones), each with search and status/type/department/role filters.
Filters run in the browser on the full list (the API has no pagination yet). Every request sends a hardcoded
`X-Acting-User-Id: 1` (dev only, ADR-0003).
