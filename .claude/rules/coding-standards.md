---
paths:
  - "src/**/*.cs"
---

# Coding standards

- Style is enforced by `.editorconfig`, analyzers and `TreatWarningsAsErrors`; run `dotnet format` instead of debating style.
- File-scoped namespaces, nullable enabled, `sealed` classes, primary constructors for DI.
- Two projects: `Orchestrator.Api` (controllers, per-resource service + DTOs, errors, middleware) references
  `Orchestrator.Infrastructure` (entities, `DbContext`, EF configuration, migrations). Add a Domain/Application layer only
  when real business logic outgrows the per-resource service, and record it in `docs/decisions.md`.
- Controllers: one per resource in `Api/Controllers`, `[ApiController]`, thin: call the resource's service. DTOs are
  records next to the service; never return EF entities over HTTP.
- Routes are versioned: `[ApiVersion(n)]` + `api/v{version:apiVersion}/<resource>`.
- Accept `CancellationToken` on every async action and I/O method.
- Errors: throw `NotFoundException` / `ConflictException`; `GlobalExceptionHandler` maps them (and DB unique violations)
  to problem+json. Never swallow exceptions, never put exception text in 500 bodies.
- Request DTOs: records with `required` init properties; DataAnnotations on the properties (constructor-parameter attributes are invisible to OpenAPI).
- EF: one `IEntityTypeConfiguration<T>` per entity; let the database enforce uniqueness (unique indexes, the
  `case_insensitive` collation) instead of pre-check queries; never edit an applied migration; no destructive migration
  without approval. Generated migration files are exempt from analyzers.
- No new package, project or abstraction without a concrete need; no placeholder files.
- No comments by default, and no XML docs at all (see CLAUDE.md). Only a crucial non-obvious why gets one line.
- **Every table entity inherits `BaseEntity`** (`Id`, `InsertedAt`/`ById`, `LastUpdatedAt`/`ById`, `IsActive`). Never redeclare
  those members. `Id` is `int` identity starting at 1000 (set for all `BaseEntity` types in `OrchestratorDbContext`);
  `SaveChangesAsync` stamps the two timestamps. Seed rows use ids below 1000. The `...ById` columns are plain nullable
  ints until a users table exists; then add the FK.
- **Relationships use PK/FK only**: child has `int <Parent>Id` pointing at the parent `Id` (navigation + `HasForeignKey`).
  Never join or relate tables on Name, Code or other columns; those stay unique lookups, not keys. Expose ids as `int` in routes (`{id:int}`) and DTOs.
