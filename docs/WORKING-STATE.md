# Working State

Short-lived. Delete or rewrite when the work it describes is done. Decisions live in `decisions.md`, open questions in `requirements.md`.

**Status:** Orders phases 1 to 3 done (UC-008, UC-009). Schema review in progress: see docs/TODO-schema-review.md (item 2 done; 8 and 10 next). Migration `OrderItemSpecs` is not yet applied to the dev database. Earnings are a dummy (docs/TODO-earnings.md).
Migrations through `UserDepartments` exist; apply them to the dev database with `dotnet ef database update`.

**Database:** the existing Postgres container on host port 5432, database `OrchStore`, schema `biz`, user `sa`. The repo ships no Postgres container.

**Risks:** dev-phase auth only (password sign-in, no OTP/MFA, seeded Super Admin): finish `docs/TODO-auth-end-of-dev.md` before any real deployment. Deactivating a
department does not check its users. k6 thresholds are provisional. The `case_insensitive` collation needs ICU (`postgres:17-alpine` works).

**Not yet run:** `.http` files in an IDE client, the GitHub Actions workflow, k6 load/spike/soak.

**Next:** owner reviews; phase 2 (orders, notes, completion, notifications, client registration, employee read-scoping); auth end-of-dev TODO.
