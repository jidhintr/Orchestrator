# Working State

Short-lived. Delete or rewrite when the work it describes is done. Decisions live in `decisions.md`, open questions in `requirements.md`.

**Status:** Department module (UC-001) implemented and slimmed down (ponytail review, 12 cuts applied 2026-10-21).
Application/Domain projects removed; Api + Infrastructure only.

**Database:** the existing Postgres container on host port 5432, database `OrchStore`, schema `biz`, user `sa`. The repo no longer ships its own Postgres container.
`InitialDepartments` was regenerated (schema `biz`, collation in `public`); an `OrchStore` created from an older version must be dropped first.

**Not yet run:** `.http` in an IDE client, the GitHub Actions workflow, k6 load/spike/soak, concurrent-duplicate scenario.

**Risks:** write endpoints are anonymous (do not expose beyond local development); deactivation does not check for
employees yet; k6 thresholds are provisional; the `case_insensitive` collation needs ICU in the Postgres image
(`postgres:17-alpine` works).

**Next:** owner reviews, answers the open questions in `requirements.md`, and names the next module.
