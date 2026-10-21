# Decisions

Add a short section for each significant choice (context, decision, alternatives, consequences). Skip small choices.

## ADR-0001: Foundation stack (2026-10-21, owner)
- ASP.NET Core MVC controllers (owner preferred them over Minimal APIs), one per resource.
- Two source projects, Api and Infrastructure, plus `Orchestrator.Specs`. Application/Domain layers were removed on 2026-10-21 (no logic worth a layer yet); reintroduce them when business rules outgrow the per-resource service.
- PostgreSQL via EF Core + Npgsql, code-first migrations. OpenAPI + Scalar for docs. GitHub Actions for CI.
- Tests: Reqnroll (Gherkin) on xUnit v3, in-process API against a Testcontainers Postgres; k6 for load/spike/soak.
  Unit, integration and end-to-end suites deliberately deferred by the owner.
- Quality: warnings as errors, analyzers `latest-recommended`, `dotnet format` enforced. Authentication deferred.
- Rejected: Minimal APIs, Dapper, NUnit, a shared local database for tests.

## ADR-0002: API conventions from the Department module (2026-10-21, owner approved defaults)
- **Versioning:** URL segment `/api/v{n}/...` (Asp.Versioning), mandatory; unknown versions -> 404. `/api/health` is an unversioned `MapHealthChecks` endpoint (plain text `Healthy`/`Unhealthy`, 200/503).
- **Errors:** `GlobalExceptionHandler` maps `NotFoundException` -> 404, `ConflictException` -> 409, anything else -> 500
  with a generic body. All errors are problem+json with `traceId` and `correlationId`; `[ApiController]` yields 400.
- **Middleware:** `CorrelationIdMiddleware` echoes a safe `X-Correlation-Id` or generates one, and adds it to log scopes.
- **Action filter:** none; `[ApiController]` already validates. Add one only for a concrete cross-cutting need.
- **Structure:** one service class per resource (`DepartmentService`) using `DbContext` directly; DTO records beside it;
  business rule violations throw `ConflictException`/`NotFoundException`.
- **Persistence:** `BaseEntity` for every table (int identity PK from 1000, audit columns, `IsActive`), PK/FK joins only, seeds use ids 1-999; unique indexes on `Code` and `Name`, case-insensitive uniqueness by the database (ICU collation `case_insensitive` on `Name`); unique violations map to 409,
  seed via `HasData` in migration `InitialDepartments`, soft deactivation only, migrations applied explicitly.
- **Auth:** none yet; Department endpoints are anonymous. No policies are invented before auth is decided.
- **Assumed, awaiting confirmation:** seed codes `SUPER_ADMIN`, `ADMIN`, `ACCOUNTS`, `QA`, `DELIVERY`, `CLIENTS`; all six
  seeded rows are system rows; code = 2-32 chars of `A-Z0-9_`, immutable; name <= 100, description <= 500;
  no reactivation endpoint; no pagination.
- **Consequence:** later modules follow this shape; deactivation cannot yet check for employees.
