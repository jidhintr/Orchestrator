# Architecture Overview

Status: foundation only. No business features exist yet.

## Layers (dependencies point inward)
```
Api -> Application -> Domain
Api -> Infrastructure -> Application
```
| Project | Responsibility | Entry points |
|---|---|---|
| `Orchestrator.Api` | HTTP surface: MVC controllers (one file per resource), contracts in `Contracts/`, OpenAPI + Scalar, composition root | `Program.cs`, `Controllers/*Controller.cs` |
| `Orchestrator.Application` | Use-case orchestration and ports (interfaces). Empty until the first use case | none |
| `Orchestrator.Domain` | Business rules and entities. Empty until the first use case | none |
| `Orchestrator.Infrastructure` | EF Core `OrchestratorDbContext`, Npgsql, health checks, `AddInfrastructure()` | `DependencyInjection.cs`, `Persistence/` |

Controllers stay thin: bind, call Application, map the result. Business rules never live in controllers.

## Data flow (health, the only flow today)
`GET /api/health` -> `HealthController` -> `HealthCheckService` -> EF Core check `postgres` -> 200 `Healthy`,
or 503 with the failing checks.

## API documentation
Controllers, XML doc comments and `[ProducesResponseType]` generate the OpenAPI document (`/openapi/v1.json`);
Scalar renders it at `/scalar/v1`. Both are enabled in Development only.

## Persistence
PostgreSQL via EF Core code-first. No entities or migrations yet. Migrations will live in
`Orchestrator.Infrastructure/Persistence/Migrations`.

## Open items
Authentication/authorization, UI framework, deployment target, API versioning and the observability stack are
undecided. See [WORKING-STATE](../WORKING-STATE.md).
