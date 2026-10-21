---
paths:
  - "src/Orchestrator.Api/**"
  - "http/**"
  - "k6/**"
---

# API artifacts: Scalar docs, `.http`, k6 (kept in sync, always)

Adding or changing an endpoint is **not done** until all four are updated.

1. **Scalar/OpenAPI docs** - XML `<summary>`, `<param>`, `<response code>` comments on the action;
   `[ProducesResponseType<T>(status)]` for every status it can return (including 400/404/503 where real);
   record contracts with `<param>` docs. Verify `/openapi/v1.json` contains the endpoint.
2. **`http/<resource>.http`** - one file per controller. Each request has a `###` title, a comment with the expected
   status/body, success AND failure examples. Use `{{baseUrl}}` from `http/http-client.env.json`. No secrets or tokens
   (use `{{variable}}` placeholders defined in an untracked `http-client.env.json` private file once auth exists).
3. **`k6/<resource>.js`** - copy the structure of `k6/health.js` (profiles smoke/load/spike/soak, `lib/config.js`
   thresholds, `check()` on status and body). Cover each new endpoint in the happy path; add write endpoints only with
   data that is safe to create repeatedly.
4. **Tests + coverage map** - unit/integration tests, and a row in `docs/testing/coverage-map.md`.

Verify before finishing: build passes, `.http` request returns the documented result against a running API (or `curl`
equivalent; state which), `k6 run k6/<resource>.js` smoke passes. Anything not run is reported as unverified.
Removing or renaming an endpoint removes/updates it in all four places. Breaking contract changes need user approval.
