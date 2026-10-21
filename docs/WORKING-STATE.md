# Working State

Short-lived. Delete or rewrite when the work it describes is done. Decisions live in `decisions.md`, open questions in `requirements.md`.

**Status:** Departments (UC-001), users with multi-department/roles/rank rules (UC-002) and roles (UC-003) implemented; 89 scenarios pass.
Migrations through `UserDepartments` exist; apply them to the dev database with `dotnet ef database update`.

**Database:** the existing Postgres container on host port 5432, database `OrchStore`, schema `biz`, user `sa`. The repo ships no Postgres container.

**Risks:** the `X-Acting-User-Id` header is trusted (no real authentication): never expose beyond local development. Deactivating a
department does not check its users. k6 thresholds are provisional. The `case_insensitive` collation needs ICU (`postgres:17-alpine` works).

**Not yet run:** `.http` files in an IDE client, the GitHub Actions workflow, k6 load/spike/soak.

**Next:** owner reviews; phase 2 (orders, notes, completion, notifications, client registration, employee read-scoping); real authentication.
