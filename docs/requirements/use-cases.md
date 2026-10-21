# Use Cases

## Product context (stated by the owner, 2026-10-21)
Orchestrator tracks progress of the assembly line of a printing press. Users: clients, managers, accounts, and other
department staff. Efforts are logged per catalog and per job step, to analyse performance/efficiency per employee and
job type/catalog, and to calculate employee earnings.
- A client places an order directly or via the reception team.
- The order passes an approval chain (custom per company, defined by admin). After finance or the core decision maker
  approves, the assembly line picks it up and moves it through stages one by one.
- Progress, time and cost are tracked per stage. The succeeding department / job type (JT) is notified.

## Module 1: Department (rules stated by the owner)
- A department has employees and managers. All employees of a department share one role and the same visible pages.
  Managers have extra pages (approve, deny, alter orders; manage employees of their own department only).
- A manager sees/modifies only employees of their own department and may add any employee to it.
- A manager can alert, add a note, or change priority of an order (e.g. QA manager cancels an order when quality fails;
  cutting manager raises priority to make the preceding department speed up).
- Employees can only add notes to an order (no priority change) and complete an ongoing order under their own name.
- A manager can complete, cancel or approve the status of an order belonging to their department or their employees.
- Approval chains are created by admin / super admin only; managers cannot change them.
- Seed departments in the first migration: super admin, admin, accounts, QA, delivery. A **clients** department must
  exist for external users: read-only, own orders' progress only, no commission, profit or internal details.
- Web UI pages are allowed per department; only super admin / admin assign pages to departments and their managers.

## Not yet defined (ask, do not guess)
Order/catalog/job-step models, earnings formula, notification channel, company (tenant) model, authentication.

## Template for each use case
ID, actor, preconditions, main/alternative/failure flows, authorization, data effects, acceptance criteria, required tests.

## UC-000: Platform health (technical, not business)
Actor: operator, load balancer, CI smoke test.
- Main: `GET /api/health` returns 200 `{status: "Healthy", checks: [...]}` when PostgreSQL is reachable.
- Failure: PostgreSQL unreachable -> 503 with `status: "Unhealthy"`. Never a false 200.
- Authorization: none for now (auth undecided; revisit when auth is introduced).
- Acceptance: scenarios and tests are listed in [coverage-map](../testing/coverage-map.md).
