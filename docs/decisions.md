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
  department sit in the seeded system department **Unassigned** (id 7), and leave it when given another. Seeded departments:
  Administratives, Accounting, Quality Check, Customers, Unassigned. Customers (external) is exclusive: no other department.
  Seeded roles (ids 1-5): Super Admin, Admin,
  Manager, Employee, Client; the Customers department carries Client, Administratives carries Admin. Only the seeded Super Admin user (direct role) and Administratives
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
  Rights are checked before request validation (`[AdminOnly]`). Reading needs a signed-in user (ADR-0005).
- **Acting user:** superseded by ADR-0005 (real sign-in). Unknown or inactive -> 401; not allowed -> 403 still holds.
- **Out of scope until phase 2:** orders, notes, completion and notifications; employee read-scoping (own profile only);
  client registration. Role endpoints beyond CRUD (e.g. approval chains, UI pages) are not modelled.
- **Consequences:** `users.DepartmentId` was replaced by the join table (migration `UserDepartments` copies existing rows);
  the audit `...ById` columns still have no FK to `users`.

## ADR-0004: UI is React + Vite PWA (2026-10-21, owner)
- **Decision:** React + TypeScript on Vite in `ui/`, installable as a PWA for phones; Capacitor can wrap it later. SignalR client for phase-3 notifications.
- **Dev wiring:** Vite proxies `/api` to the API, so the API has no CORS configuration. The UI signs in through `/api/v1/auth` (ADR-0005).
- **Filtering:** client-side over the full list (no pagination in the API); move to query parameters when pagination lands.
- **Rejected:** Angular (more ceremony for a CRUD back office), Flutter/React Native (second toolchain).

## ADR-0004: Four-project clean architecture (2026-10-22, owner)
- **Context:** Infrastructure held entities, DbContext, EF configuration and migrations while Api held controllers plus every
  service, DTO and access rule, so both grew heavy (supersedes the "Application/Domain removed" note in ADR-0001).
- **Decision:** `Domain` (entities, `SystemIds`, `Rank`) <- `Application` (services, DTOs, `IAppDbContext`, `ICurrentUserId`,
  `ActingUser`, exceptions) <- `Infrastructure` (`OrchestratorDbContext`, configurations, migrations, `AddInfrastructure`);
  `Api` composes them. `HttpCurrentUserId` (reads `X-Acting-User-Id`) lives in Api; Postgres unique/FK violations are translated to
  `ConflictException`/`BadRequestException` in `OrchestratorDbContext.SaveChangesAsync`, so Api has no Npgsql dependency.
- **Consequences:** Application still uses EF Core abstractions (`Include`, `ToListAsync`) but no provider; no repository layer.
  Domain enums carry a `System.Text.Json` converter attribute. Migrations keep their namespace; only the model snapshot's entity
  type names changed to `Orchestrator.Domain.*` (no schema change; `has-pending-model-changes` is clean).

## ADR-0005: Password sign-in with JWT, everything behind auth (2026-10-22, owner)
- **Decision:** users sign in with email or registered mobile plus a password. A manager, admin or super admin sets or resets a user's password (`PUT /users/{id}/password`, or `password` when creating); users change their own with `auth/change-password`. No OTP in the dev phase.
- **Tokens:** JWT access token (15 min, HS256, key `Auth:JwtKey` from user-secrets/env) in the `Authorization: Bearer` header; rotating refresh token (7 days, hashed at rest, reuse revokes the session) in an httpOnly, Secure, SameSite=Strict cookie scoped to `/api/v1/auth`. The user is re-checked in the database on every request, so deactivation takes effect at once.
- **Passwords:** PBKDF2-SHA256 (600k iterations), 10 to 128 characters, one generic "Invalid credentials" message, lockout for 15 minutes after 5 failures, sign in rate limited per address.
- **Storage:** schema `identity` (`user_credentials`, `refresh_tokens`), linked to `biz.users` by FK. `users.Mobile` is unique because it is a login.
- **Access:** every endpoint needs a token except `/api/health`, `/api/health/live`, `/api/health/ready`, sign in, refresh, logout (and Scalar/OpenAPI in Development). Rank rules (`CurrentUser`, `[AdminOnly]`) are unchanged.
- **Dev phase:** only the seeded Super Admin exists; in Development its password comes from `Auth:DevSeedPassword`. Remove it, add OTP/MFA and the rest at the end of dev: see `docs/TODO-auth-end-of-dev.md`.
- **Supersedes:** ADR-0003 acting-user header.

## ADR-0006: Orders, Estimation department and client login link (2026-10-22, owner)
- **Decision:** `Order` references `Client` and `Catalog`, holds `Priority` (Normal, High, Premium, UltraHigh) and `Status` (PendingApproval, InProgress, OnHold, Completed, Cancelled). `order_stages` copies the catalog steps at placement; `order_event_history` is append-only.
- **Estimation (ED):** seeded system department id 8 (`Estimation`), the return target of a cancelled order. Name and code are placeholders pending owner confirmation.
- **Client login:** `clients.UserId` (nullable, unique FK to `users`) ties a Client-role user to their client; client users are scoped to that client's orders.
- **Open:** which department's manager may act on a stage (no job-type to department mapping yet), so cancel and priority requests are Manager-or-above anywhere for now.

## ADR-0007: Order quote snapshot (2026-10-22, owner)
- **Decision:** `orders` stores `Quantity`, `UnitPrice`, `CatalogDiscountPercent`, `ClientDiscountPercent` and `QuotedTotal` at placement, so later catalog or tier changes do not rewrite a quote. `QuotedTotal` = `UnitPrice` x `Quantity` x (1 - catalog %) x (1 - client %), rounded to 2 decimals half away from zero. Estimation may override it before approval (`POST orders/{id}/quote`, event `QuoteAdjusted`).
- **Open:** tax, labor cost in the quote, per-job-type pricing, and what Approve does to the quote (approval flow is next).
