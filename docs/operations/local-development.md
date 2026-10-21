# Local Development

1. `cp .env.example .env`, then set `ORCHESTRATOR_DB_PASSWORD` (and `ORCHESTRATOR_DB_PORT` if 5433 is taken).
   5432 is not the default because it is commonly occupied.
2. `docker compose up -d --wait`
3. Keep the connection string out of source control:
   `dotnet user-secrets set "ConnectionStrings:Orchestrator" "Host=localhost;Port=5433;Database=orchestrator;Username=orchestrator;Password=..." --project src/Orchestrator.Api`
   (or env var `ConnectionStrings__Orchestrator`). The app fails fast at startup if it is missing.
4. `dotnet run --project src/Orchestrator.Api --launch-profile http` -> http://localhost:5176
5. Docs at `/scalar/v1`. Requests: open `http/*.http` and pick the `local` environment (`http/http-client.env.json`).
6. Load test: `k6 run k6/health.js` (`-e PROFILE=load|spike|soak`, `-e BASE_URL=...`).

## Troubleshooting
- `port is already allocated`: another container or service holds the port; change `ORCHESTRATOR_DB_PORT`.
- Integration tests cannot start: Docker must be running (Testcontainers).
- Use `dotnet test --solution Orchestrator.slnx` (xUnit v3 on .NET 10 uses Microsoft.Testing.Platform).

Deployment: not yet defined.
