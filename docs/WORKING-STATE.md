# Working State

Short-lived. Delete or rewrite when the work it describes is done. Decisions live in `decisions.md`, open questions in `requirements.md`.

**Status:** Orders first pass added (UC-008, ADR-0006); migration `Orders` not yet applied to the dev database. 4 pre-existing failures: broken-DB tests fail because dev Super Admin seeding hits the unreachable DB at startup.
Migrations through `UserDepartments` exist; apply them to the dev database with `dotnet ef database update`.

**Database:** the existing Postgres container on host port 5432, database `OrchStore`, schema `biz`, user `sa`. The repo ships no Postgres container.

**Risks:** dev-phase auth only (password sign-in, no OTP/MFA, seeded Super Admin): finish `docs/TODO-auth-end-of-dev.md` before any real deployment. Deactivating a
department does not check its users. k6 thresholds are provisional. The `case_insensitive` collation needs ICU (`postgres:17-alpine` works).

**Not yet run:** `.http` files in an IDE client, the GitHub Actions workflow, k6 load/spike/soak.

**Next:** owner reviews; phase 2 (orders, notes, completion, notifications, client registration, employee read-scoping); auth end-of-dev TODO.
