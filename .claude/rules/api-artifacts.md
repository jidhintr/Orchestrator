---
paths:
  - "src/Orchestrator.Api/**"
  - "http/**"
  - "k6/**"
---

# API artifacts: Scalar docs, `.http`, k6 (kept in sync, always)

An endpoint change is **not done** until all four are updated.

1. **Scalar/OpenAPI** - versioned route; no XML doc comments. Per action: `[EndpointSummary("...")]` (one line), `[EndpointDescription("...")]` only for non-obvious behavior (409 reasons, immutability, soft delete);
   `[ProducesResponseType<T>(status)]` for every status it can return. Request DTOs are records with `required` init properties and DataAnnotations on the properties (not constructor parameters) so limits show in the schema.
   Verify `/openapi/v1.json` lists the endpoint. This is the API contract; do not duplicate it in Markdown.
2. **`http/<resource>.http`** - one file per controller; a `###` title per request;
   success AND failure examples; `{{baseUrl}}` from `http/http-client.env.json`; no secrets or tokens.
3. **`k6/<resource>.js`** - follow `k6/departments.js` (profiles smoke/load/spike/soak, `lib/config.js` thresholds,
   `check()` on status and body, intentional 4xx declared via `expectedStatuses`). Write endpoints only with data safe to repeat.
4. **Gherkin scenarios** in `tests/Orchestrator.Specs/Features`, tagged `@UC-xxx`.

Verify: build passes, the `.http` outcomes hold against a running API (or `curl` equivalent; say which), `k6 run` smoke
passes. Anything not run is reported as unverified. Removing or renaming an endpoint updates all four. Breaking
contract changes need user approval.
