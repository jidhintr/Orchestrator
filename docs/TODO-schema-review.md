# Schema review tracker (printpack_db.sql vs Orchestrator)

Owner decisions of 2026-10-26. Work one item at a time; the owner says "next" to move on. "Don't take" suggestions from the review (JSON stages, single approval pointer, single discount amount, text[] permissions, SQL functions and triggers, cascade deletes, OTP login) are dropped for good.

| # | Item | Decision | Status |
|---|---|---|---|
| 1 | `stage_work` and `operator_rates` (earnings) | Next phase; earnings stay a dummy ([TODO-earnings.md](TODO-earnings.md)) | Deferred |
| 2 | Item description and specs on order items (API, model, DB, UI) | Do now | Done |
| 3 | Billing: VAT, invoice, payments, customer credit days and TRN | Next phase | Deferred |
| 4 | `other_cost` on the order | Ignore | Dropped |
| 5 | Customer-facing step labels (`customer_label`, `show_to_customer`) | Last phase | Deferred |
| 6 | Delivery: delivery note, delivered date, Delivered and Closed statuses (needs UI input) | Next phase | Deferred |
| 7 | Comments and attachments | Not wanted | Dropped |
| 8 | Readable order number (SO-00001) | Do now | Done |
| 9 | Approval `send_back` action and `skipped` step status | Maybe last phase | Deferred |
| 10 | Catalog: category, seasonal availability, clone, approve before active | Do now | Done (migration `CategoriesAndCatalogApproval` not yet applied to dev) |
| 11 | Approver by specific user | Only if the business asks | Dropped for now |
