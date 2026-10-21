# Use-Case to Test Map

"Verified" means the test was executed successfully (last local run: 2026-10-21).

| Use case | Scenario | Level | Automated test | Status |
|---|---|---|---|---|
| UC-000 | Healthy dependencies -> 200 | Unit | `UnitTests/HealthControllerTests.Get_WhenAllChecksHealthy_Returns200WithChecks` | Verified |
| UC-000 | Unhealthy or degraded -> 503 | Unit | `UnitTests/HealthControllerTests.Get_WhenAnyCheckNotHealthy_Returns503` | Verified |
| UC-000 | Postgres reachable -> 200 | Integration | `IntegrationTests/HealthEndpointTests.GetHealth_WhenPostgresReachable_Returns200` | Verified |
| UC-000 | Postgres down -> 503 | Integration | `IntegrationTests/HealthEndpointFailureTests.GetHealth_WhenPostgresDown_Returns503NotFalseSuccess` | Verified |
| UC-000 | OpenAPI document lists endpoint | Integration | `IntegrationTests/HealthEndpointTests.GetOpenApiDocument_IsServedInDevelopment` | Verified |
| UC-000 | Smoke load | k6 | `k6/health.js` (smoke) | Verified locally; CI job not yet run |
| UC-000 | Load and spike | k6 | `k6/health.js` (`PROFILE=load` / `spike`) | Pending (not run) |
