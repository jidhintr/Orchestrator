---
paths:
  - "src/**/*.cs"
---

# Coding standards

- Style is enforced by `.editorconfig`, analyzers and `TreatWarningsAsErrors`; run `dotnet format` instead of debating style.
- File-scoped namespaces, nullable enabled, `sealed` classes by default, primary constructors for DI.
- Dependencies point inward only: Domain has no project references; Application references Domain; Infrastructure
  references Application; Api references Application + Infrastructure. Do not break this.
- Controllers: one file per resource in `Api/Controllers`, `[ApiController]`, thin. Request/response types live in
  `Api/Contracts` as records; never expose EF entities or Domain types directly over HTTP.
- Accept `CancellationToken` on every async action and I/O method.
- Validate input at the API boundary; return `ProblemDetails` for errors. Do not swallow exceptions or report false success.
- EF Core: configuration via `IEntityTypeConfiguration<T>` in Infrastructure; migrations are version-controlled in
  `Infrastructure/Persistence/Migrations`; never edit an applied migration; no destructive migration without
  explicit approval and a rollback note.
- Do not add a NuGet package, project or abstraction without a concrete need. Search for existing code first.
- No placeholder classes or empty files.
