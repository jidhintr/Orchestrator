# Requirements

Executable behavior lives in `tests/Orchestrator.Specs/Features/*.feature` (tagged `@UC-xxx`); API contracts live in
Scalar/OpenAPI. This file holds intent, rules and open questions only.

## Product context (owner, 2026-10-21)
Tracks progress of the assembly line of a printing press. Users: clients, managers, accounts and other department staff.
Efforts are logged per catalog and job step, to analyse performance per employee and job type/catalog and calculate
employee earnings.
- A client places an order directly or via the reception team.
- The order passes an approval chain (custom per company, defined by admin). After finance or the core decision maker
  approves, the assembly line moves it through stages one by one.
- Progress, time and cost are tracked per stage; the succeeding department / job type (JT) is notified.

## Module 1: Department rules (owner)
- A department has employees and managers. All employees of a department share one role and the same visible pages;
  managers have extra pages (approve, deny, alter orders; manage employees of their own department only).
- A manager sees/modifies only their own department's employees and may add any employee to it.
- A manager can alert, add a note, or change priority of an order (QA manager cancels on quality failure; cutting
  manager raises priority to speed up the preceding department).
- Employees can only add notes (no priority change) and complete an ongoing order under their own name.
- A manager can complete, cancel or approve orders belonging to their department or employees.
- Approval chains are created by admin / super admin only.
- Seed departments: super admin, admin, accounts, QA, delivery, plus **clients** (external, read-only, own orders'
  progress only; no commission, profit or internal details).
- UI pages are allowed per department; only super admin / admin assign pages to departments and their managers.

## Use cases
**UC-000 Platform health** (technical): `GET /api/health` returns 200 when PostgreSQL is reachable, 503 when not.
Never a false 200. Body is plain text. Anonymous. Feature: `Health.feature` (also covers the correlation-id header).

**UC-001 Manage departments** (implemented; `Department.feature`): list, view, create, rename/describe and soft-deactivate
departments; six system departments are seeded and protected from rename/deactivation. Failures: invalid input -> 400
(nothing saved), duplicate name/code -> 409, unknown id -> 404, unexpected error -> standard 500 without internals.
Authorization is not enforced yet (auth undecided).

## Open questions (ask the owner, do not guess)
- Who may call each Department operation; should deactivation be blocked while a department has employees; reactivation?
- Confirm assumed seed codes and that all six seeded rows are system rows (see decisions).
- Authentication, Company/tenant model, UI framework, SLOs (k6 thresholds are provisional), deployment target.
- Order, catalog, job-step models, earnings formula, notification channel.

## Deferred cross-module scenarios (write when Employee and Role modules exist; no placeholders before)
- A department manager adds an employee to their own department.
- The new employee receives the department's assigned roles.
- A manager cannot manage employees of another department.
- Employees cannot assign themselves roles or permissions they are not authorized to receive.
