# Design System P0 Gate

Feature teams do not scale until these components exist in production React/TypeScript and Storybook.

## Foundations
- semantic tokens
- light/dark theme
- typography
- spacing/radius/border/elevation
- focus
- density
- motion tokens
- RTL
- icon registry

## Shell / Navigation
- AppShell
- Sidebar
- Topbar
- ContextSwitcher
- Breadcrumb
- Tabs
- PageHeader
- PageToolbar
- SectionHeader
- CommandPalette
- QuickCreate baseline

## Controls / Forms
- Button
- IconButton
- Field
- Input
- Number/Currency input baseline
- Textarea
- Select
- ComboBox
- Checkbox
- Radio
- Switch
- Date/effective-date baseline

## Feedback / Overlays
- Status/Badge
- Alert/Banner
- Toast
- Tooltip
- Popover
- Menu
- Dialog
- Drawer
- Skeleton
- EmptyState
- NoResults
- ErrorState
- AccessDenied
- ReadOnly/Locked/Finalized treatment

## Data
- Money
- SensitiveValue
- Pagination
- DataGrid wrapper baseline
- FilterBar
- SavedViews
- ColumnChooser
- DensitySwitcher
- BulkActionBar
- row action / preview conventions

## Storybook matrix

Every P0 shared component must cover as relevant:
- default
- hover/focus
- disabled
- loading
- error
- read-only
- light/dark
- LTR/RTL
- long English
- Arabic
- reduced motion
- compact/standard density
- keyboard interaction
- a11y checks

## Acceptance gate

Before Phase 2:
- Storybook builds in CI
- axe gate passes
- RTL snapshots pass
- shared component APIs are typed
- feature modules do not import vendor UI engines directly
- ZainXDataGrid theme/API is accepted
