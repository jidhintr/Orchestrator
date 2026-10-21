# Working State

Short-lived. Delete or rewrite when the work it describes is done. Decisions live in `decisions.md`, open questions in `requirements.md`.

**Status:** Department module (UC-001) implemented and slimmed down (ponytail review, 12 cuts applied 2026-10-21).
Application/Domain projects removed; Api + Infrastructure only.

**Local dev database needs a reset once:** the migration was regenerated (int ids from 1000, `BaseEntity`, case-insensitive name collation), so a database
created from the old `InitialDepartments` will not match. Run `docker compose down -v` then `docker compose up -d --wait`
and `dotnet ef database update ...` (see README).

**Not yet run:** `.http` in an IDE client, the GitHub Actions workflow, k6 load/spike/soak, concurrent-duplicate scenario.

**Risks:** write endpoints are anonymous (do not expose beyond local development); deactivation does not check for
employees yet; k6 thresholds are provisional; the `case_insensitive` collation needs ICU in the Postgres image
(`postgres:17-alpine` works).

**Next:** owner reviews, answers the open questions in `requirements.md`, and names the next module.
