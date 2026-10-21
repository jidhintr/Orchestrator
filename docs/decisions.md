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

## ADR-0003: Users, departments, roles and who may change whom (2026-10-21, owner)
- **Model:** a user belongs to one or more departments (`user_departments`); a department carries roles (`department_roles`);
  a user may also hold extra roles (`user_roles`). Effective roles = department roles + the user's own. Users with no
  department sit in the seeded system department **Unassigned** (id 7). Seeded roles (ids 1-5): Super Admin, Admin,
  Manager, Employee, Client; the Clients department carries Client. Only the Super Admin and Admin users/departments
  are seeded with roles. Fixed ids live in `SystemIds`.
- **Rank** (from effective roles): Super Admin 4 > Admin 3 > Manager 2 > Employee/Client/custom 1. Nobody changes a user
  at or above their own rank (a Super Admin may change anyone), and nobody can give a role or department that would lift a
  user to their own rank or higher. So admins cannot create or edit admins or super admins, and employees and clients
  change nobody, not even themselves.
- **Managers:** work only with users who share one of their departments. New users default to the manager's own
  departments, a manager may only use those departments, and only roles that belong to them (and below Manager).
  Replacing departments or roles only touches what the manager may assign; the user keeps the rest. Unassigning leaves
  the user in Unassigned (or their other departments), after which the manager can no longer manage them.
- **Admin-only:** creating, changing, disabling and deleting roles and departments, and setting a department's roles.
  Rights are checked before request validation (`[AdminOnly]`). Reads are open.
- **Acting user (temporary):** until authentication exists, the caller names itself in `X-Acting-User-Id`. Missing,
  invalid, unknown or inactive -> 401; not allowed -> 403. The header is trusted, so this is NOT security; replace
  `ActingUser` when auth is decided. Specs default to the seeded super admin (id 1) and switch with `I act as "<name>"`.
- **Out of scope until phase 2:** orders, notes, completion and notifications; employee read-scoping (own profile only);
  client registration. Role endpoints beyond CRUD (e.g. approval chains, UI pages) are not modelled.
- **Consequences:** `users.DepartmentId` was replaced by the join table (migration `UserDepartments` copies existing rows);
  the audit `...ById` columns still have no FK to `users`.

## ADR-0004: UI is React + Vite PWA (2026-10-21, owner)
- **Decision:** React + TypeScript on Vite in `ui/`, installable as a PWA for phones; Capacitor can wrap it later. SignalR client for phase-3 notifications.
- **Dev wiring:** Vite proxies `/api` to the API, so the API has no CORS configuration. The UI hardcodes `X-Acting-User-Id: 1` until auth lands.
- **Filtering:** client-side over the full list (no pagination in the API); move to query parameters when pagination lands.
- **Rejected:** Angular (more ceremony for a CRUD back office), Flutter/React Native (second toolchain).
