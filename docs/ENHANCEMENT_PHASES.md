# BookStore Enhancement Phases

This plan covers the requested application enhancements and intentionally excludes cloud/server sync and deployment work.

## Phase 1 - Connected Dashboard

- Make dashboard metric cards clickable.
- Route sales, receipts, profit, discounts, inventory value, low stock, and out-of-stock metrics to the real screens behind the numbers.
- Hide or disable dashboard actions based on the current user's permissions.
- Keep dashboard data dynamic and backed by the existing report and inventory handlers.

## Phase 2 - POS Workflow Depth

- Keep scanner focus stable for fast cashier work.
- Add clear keyboard-friendly controls for hold, resume, cancel, quantity change, exact payment, and quick cash.
- Add return/refund flow with permission checks and inventory restoration.
- Improve receipt-copy selection for customer, store, and accounting copies.

## Phase 3 - Notifications Center

- Add actionable alerts for low stock, out of stock, overdue backup, suspicious login activity, missing barcode/category/supplier, and daily sales status.
- Connect each notification to the relevant screen.
- Persist read/unread state locally.

## Phase 4 - Role-Based UI Control

- Hide commands that a user cannot execute.
- Keep module navigation, toolbar commands, and dashboard actions consistent with permissions.
- Add tests around important permission-to-UI visibility rules.

## Phase 5 - Audit Trail

- Status: implemented for authentication events and the audit trail screen.
- Added a persisted `AuditLogEntries` table with indexed time, area, user, and entity lookups.
- Added database-side paging and filtering through the audit query service.
- Restricted audit access with `Audit.View`.
- Next expansion: product edits, price changes, stock changes, sale cancellation/refund, receipt reprint, backup/restore, and settings changes.

## Phase 6 - Data Quality Center

- Status: implemented as a permission-protected production screen.
- Added checks for missing barcode, missing supplier, invalid price, low margin, negative stock, and inactive products that still carry stock.
- Added summary counts and a grid of issue recommendations.
- Restricted access with `DataQuality.View`.
- Next expansion: direct row navigation to the owning product/editor screen and dashboard notification counts.

## Phase 7 - Arabic Accuracy

- Review all Arabic copy and mixed-direction layouts.
- Fix RTL alignment in grids, forms, dialogs, receipts, and reports.
- Format Arabic dates, numbers, and currency consistently.

## Phase 8 - Backup Health

- Show last backup time, backup folder, latest validation result, latest backup size, and database integrity status.
- Add one-click backup and validate actions from the dashboard/backup module.
- Keep restore protected by the exact destructive confirmation text.

## Phase 9 - Performance Hardening

- Add/verify indexes for product search, barcode lookup, sales date filters, receipt search, and inventory reports.
- Keep large report/list screens paginated.
- Avoid accidental full-table loads in UI view models.
- Add regression tests for SQLite value-object query translation.
