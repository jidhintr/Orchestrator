---
paths:
  - "ui/**"
---

# UI rules (one look across every page)

- Every list page follows the Departments page: filter bar, grid via `useGrid` (`ui/src/grid.tsx`: column chooser, advanced filter, number columns right-aligned), name column left-aligned, side panel via `Modal`.
- Side panels have exactly one Save button, last in the form, with Delete/Clone/Close beside it where they exist. No per-section save buttons. Description fields are one-line inputs.
- Read-only side panel shows each label and value on one horizontal line: give the detail `<Form>` `className="ro-h"` (CSS in `app.css`). Edit mode keeps labels above inputs.
- Active/inactive (or enabled/disabled) is a "switch" field inside the form (`label.switch`, name `isActive`) saved by the one Save button, never a separate button; disable it for system records. Fields that make no sense read-only (like a new-password input) get `className="edit-only"`.
- Adding items to a list (roles, employees, steps) uses the `ChipPicker` "+ Add" menu, not a select with an extra Add button; ordered lists use `OrderedPicker`.
- A new list page or side panel must follow all of the above before it is done.
