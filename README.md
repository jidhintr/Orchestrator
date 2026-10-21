# Orchestrator

.NET 10 API platform backed by PostgreSQL. A UI (Angular or Node.js, **undecided**) will join this solution later.
Business scope is not yet defined; see [docs/requirements/use-cases.md](docs/requirements/use-cases.md).

## Stack
.NET 10 / ASP.NET Core MVC controllers, EF Core + Npgsql, PostgreSQL 17, OpenAPI + [Scalar](https://scalar.com) docs,
xUnit v3 + Testcontainers, k6 load tests, `.http` request files, GitHub Actions.

## Prerequisites
.NET SDK 10.0.x (pinned in `global.json`), Docker, [k6](https://k6.io).

## Run
```bash
cp .env.example .env                 # local DB password and port (gitignored)
docker compose up -d --wait          # Postgres on localhost:5433
dotnet user-secrets set "ConnectionStrings:Orchestrator" "Host=localhost;Port=5433;Database=orchestrator;Username=orchestrator;Password=<.env password>" --project src/Orchestrator.Api
dotnet run --project src/Orchestrator.Api --launch-profile http
```
API: http://localhost:5176. Scalar docs: http://localhost:5176/scalar/v1 (Development only).

## Check
```bash
dotnet format Orchestrator.slnx --verify-no-changes
dotnet build Orchestrator.slnx
dotnet test --solution Orchestrator.slnx    # integration tests need Docker
k6 run k6/health.js                         # API must be running; -e PROFILE=load|spike|soak
```

## Layout
| Path | Purpose |
|---|---|
| `src/Orchestrator.Api` | Controllers, contracts, OpenAPI/Scalar, composition root |
| `src/Orchestrator.Application` | Use-case orchestration (empty until the first use case) |
| `src/Orchestrator.Domain` | Business rules (empty until the first use case) |
| `src/Orchestrator.Infrastructure` | EF Core `DbContext`, Postgres, DI registration |
| `tests/` | `Orchestrator.UnitTests`, `Orchestrator.IntegrationTests` |
| `http/` | `.http` request files, one per controller, kept in sync with endpoints |
| `k6/` | k6 scripts per endpoint group with smoke/load/spike/soak profiles |
| `docs/` | Architecture, requirements, testing, decisions, operations |
| `CLAUDE.md`, `.claude/` | Instructions, scoped rules and skills for Claude Code |

## Documentation
[Architecture](docs/architecture/overview.md) · [Use cases](docs/requirements/use-cases.md) ·
[Test strategy](docs/testing/test-strategy.md) · [Coverage map](docs/testing/coverage-map.md) ·
[Decisions](docs/decisions/) · [Local development](docs/operations/local-development.md) ·
[Working state](docs/WORKING-STATE.md)
