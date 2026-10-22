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

## Users, roles and access rules (owner, 2026-10-21; decided in ADR-0003, specs in `UserAccess.feature`, `Role.feature`)
1. Admin creates employees; no department given means Unassigned; an employee can be in several departments and hold any roles below Admin.
2. A manager creates employees in their own department and can unassign them (they stay in Unassigned); a manager only assigns roles that belong to their own departments.
3. A department's roles apply to everyone in it, including its manager, who may also hold extra roles.
4. Super Admin or Admin can make anyone a manager and edit roles (Admin never touches admins or super admins).
5. A user can have several roles.
6. The Clients department carries the Client role (very limited, read-only on their own orders; orders come in phase 2).
7. Nobody edits users at or above their own rank; employees edit nobody.
8. Employee actions (notes, completing orders, notifying managers) are phase 2.

## Use cases
**UC-000 Platform health** (technical): `GET /api/health` returns 200 when PostgreSQL is reachable, 503 when not.
Never a false 200. Body is plain text. Anonymous. Feature: `Health.feature` (also covers the correlation-id header).

**UC-002 Users and who may change whom** (implemented; `UserAccess.feature`) and **UC-003 Roles and department roles** (implemented; `Role.feature`): see the rules above and ADR-0003.

**UC-004 Manage catalogs** (`Catalog.feature`): list, view, create, update (replaces the ordered job type sequence), clone and
soft-deactivate catalogs. A catalog referenced by any order cannot be edited or deactivated (409; clone it instead); cloning is
always allowed. Job types are seeded placeholders (read-only list only, UC-007); Order is a stub (catalogId only). Reporting is deferred.

**UC-005 Manage clients** (`Client.feature`): list, view, create, update and set the account status (Active, PaymentPending,
Suspended, Inactive) of external clients; tier (Normal, Gold, Platinum) sets the stored discount (placeholder 0/5/10, pending
owner values). Email is unique. Deferred until orders exist: blocking orders for Suspended clients, speedups, CSV export,
print documents, client-only views.

**UC-006 Approval chains** (`ApprovalChain.feature`): an ordered list of approving departments. A default chain always exists (seeded
with no stages, editable, not deletable); a catalog may have one own chain, otherwise it follows the default. Admin or super admin
change chains; reads are open. Departments must exist, be active and appear once. Deferred until orders exist: the approval flow
itself (the department manager approves, then the assembly line picks the order up), and chains referenced by orders.

**UC-007 Job types** (`JobType.feature`): read-only list of the seeded placeholder job types, so catalog forms can pick and order steps. Create/edit stays deferred until the owner confirms the real job types.


**UC-007 Manage job types** (`JobType.feature`): list, view, create, update, soft-deactivate job types (assembly-line steps) with optional
labor cost, commission % and profit % overrides (null = use the catalog values), and list the catalogs using one. Name is unique.
Admin or super admin change them; reads are open. Deferred: job type status, assignment and the completion notification (need orders).

**UC-001 Manage departments** (implemented; `Department.feature`): list, view, create, rename/describe and soft-deactivate
departments; six system departments are seeded and protected from rename/deactivation. Failures: invalid input -> 400
(nothing saved), duplicate name/code -> 409, unknown id -> 404, unexpected error -> standard 500 without internals.
Changes are limited to admins and super admins (acting-user header, ADR-0003).

## Open questions (ask the owner, do not guess)
- Who may call each Department operation; should deactivation be blocked while a department has employees; reactivation?
- Confirm assumed seed codes and that all six seeded rows are system rows (see decisions).
- Authentication, Company/tenant model, SLOs (k6 thresholds are provisional), deployment target.
- Order, catalog, job-step models, earnings formula, notification channel.

## Deferred cross-module scenarios (write when Employee and Role modules exist; no placeholders before)
- A department manager adds an employee to their own department.
- The new employee receives the department's assigned roles.
- A manager cannot manage employees of another department.
- Employees cannot assign themselves roles or permissions they are not authorized to receive.
