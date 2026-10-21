# ADR-0001: Foundation stack and layering

- Status: Accepted
- Date: 2026-10-21
- Deciders: Repository owner

## Context
New solution: .NET 10 APIs, class libraries, a future UI (Angular or Node.js, undecided), PostgreSQL. It needs documented
endpoints, a maintained `.http` file and k6 scripts, and a test pyramid that later extends to end-to-end tests.

## Decision
- **API style:** ASP.NET Core MVC controllers, one controller file per resource.
- **Layers:** Api / Application / Domain / Infrastructure plus UnitTests and IntegrationTests.
- **Data:** PostgreSQL via EF Core + Npgsql, code-first migrations.
- **API docs:** built-in `Microsoft.AspNetCore.OpenApi` rendered by Scalar.
- **Tests:** xUnit v3; Testcontainers (real PostgreSQL) for integration tests; k6 for load/spike/soak.
- **CI:** GitHub Actions.
- **Auth:** deferred, not decided.
- **Quality:** warnings as errors, analyzers at `latest-recommended`, `dotnet format` enforced.

## Alternatives considered
Minimal APIs (owner chose controller files because they scale better for this project); Dapper; NUnit; a shared local
database for tests (less repeatable).

## Consequences
Integration tests need Docker locally and in CI. Application and Domain stay empty until a real use case justifies
content. Separate ADRs are required for auth, the UI framework and API versioning.
