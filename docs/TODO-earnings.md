# TODO: earnings and commission

Owner decision (2026-10-23): the earnings formula is not decided; it is a dummy for now.

- `order_item_job_steps.EarnedAmount` exists and is always 0. Nothing writes it.
- Each step already snapshots the inputs that are known: `LaborCost`, `CommissionPercent`, `ProfitPercent` (job type override, else catalog), the item `LineNet` and quantity, and who completed it (`CompletedByUserId`).
- [ ] Owner to define the formula (commission on labor cost or on the line net, how a reassigned or reworked step is split, what the profit percent drives).
- [ ] Rule from the spec: commission and profit are eligible only when the whole order is Completed (partial work is deferred). Compute `EarnedAmount` when the order completes (the Completed event in `OrderStepService.CompleteAsync`).
- [ ] The step discount (`SnappedStepDiscountPercent` in the owner diagram) is not modelled; confirm what it means before adding it.
- [ ] Employee earnings report and per-catalog profit report.
