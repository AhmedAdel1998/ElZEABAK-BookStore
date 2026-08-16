# Production Readiness Audit

Date: 2026-08-16
Scope: Phase 18 audit and hardening for the offline BookStore POS WPF application.

## Severity Findings

### Critical

- No critical issue remains open from this pass.

### High

- Hardcoded default administrator password hash existed in database seeding. Fixed by requiring optional first-run `Application:InitialAdmin` configuration with a BCrypt hash; no known admin credential is shipped by default.
- SQLite runtime configuration did not explicitly enforce foreign keys, busy timeout, or WAL mode during application initialization. Fixed through connection-string options and startup PRAGMAs.
- Checkout completion could be invoked concurrently by accidental double-click or overlapping command execution. Fixed with a process-local checkout concurrency guard and regression test.
- NuGet vulnerability audit found `SQLitePCLRaw.lib.e_sqlite3` 2.1.10 high severity via Infrastructure. Fixed by pinning `SQLitePCLRaw.bundle_e_sqlite3` 3.0.5 in Infrastructure and rechecking vulnerabilities.
- Relative writable paths resolved under the application/install directory. Fixed by resolving runtime data paths under `BOOKSTORE_DATA_DIR` or `%LOCALAPPDATA%\BookStore POS`.

### Medium

- Settings and some UI services still have areas where production UX needs refinement, especially full unsaved-navigation confirmation and category-specific screens.
- Hardware-dependent receipt printer and barcode scanner behavior cannot be verified without real devices.
- Historical profit accuracy remains limited because `SaleItem` stores selling price and discount but not historical purchase cost.
- Some synchronous settings access remains in the synchronous pricing interface; it is cached but should be redesigned if the pricing API becomes asynchronous.

### Low

- Some legacy appsettings log path entries remain relative, but the application host now also writes a production log sink under the resolved data root with 31 retained files.
- Publish profile is file-system based; installer packaging is documented but not implemented.

## Audit Summary

- Architecture: Project dependencies remain aligned with Clean Architecture. UI composes Application, Infrastructure, Persistence, Shared, and Reporting; Persistence depends on Domain/Shared; Infrastructure depends on Application/Shared.
- CQRS: Existing read paths use DTOs and broad reporting/persistence queries use `AsNoTracking()` where appropriate. Commands remain state-changing operations.
- Domain and transaction integrity: POS checkout validates cart, payment, product state, stock, creates sale/items/inventory transactions inside the Unit of Work transaction, commits, then attempts receipt printing. Receipt failure after commit does not roll back the sale.
- Database: Indexed barcode, invoice number, sales dates/status/customer/user/payment, product/category/stock, customers, suppliers, inventory movement, and settings key/category paths are present. SQLite foreign keys, busy timeout, and WAL are now explicitly configured.
- Security: BCrypt password hashing is used. Known default admin seed was removed. Authorization is enforced in many command handlers; additional command-level permission coverage should continue to be expanded as modules evolve.
- Backup/restore: Existing implementation uses SQLite backup API, integrity validation, SHA-256 sidecars, pre-restore backup, and `File.Replace`; real restore on a production-like copy was not executed in this pass.
- Logging: Structured logging is used. Production data-root file logging now has a 31-file retention limit. Sensitive settings changes are logged without serialized values.
- Performance: Barcode/product search indexes exist. No fake performance numbers were recorded; benchmark timing on target hardware remains required.
- Deployment: Added `ProductionWinX64.pubxml` for Release, self-contained, win-x64, no trimming.

## Production Configuration

- Set `BOOKSTORE_DATA_DIR` when the installer should use a machine-wide writable location. If unset, data is written under `%LOCALAPPDATA%\BookStore POS`.
- Configure `Application:InitialAdmin` only during first-run provisioning:
  - `Username`
  - `PasswordHash` as BCrypt
  - optional `FullName`
  - optional `Email`
- Remove the initial admin configuration after first successful provisioning.
- Installer must grant cashier users write access to Database, Logs, Backups, Exports, and Temp under the chosen data root.

## Deployment Layout

Recommended installer-owned binary folder:

```text
BookStore/
  BookStore.UI.exe
  *.dll
  appsettings.json
```

Writable data root:

```text
BookStore POS/
  Database/
  Backups/
  Logs/
  Exports/
  Temp/
```

## Not Verified

- Real 80mm thermal printer behavior.
- Real barcode scanner speed/reconnect behavior.
- Full manual production smoke test of the published executable.
- Real backup restore against a production-like database copy.
- Startup/search/checkout/report timing on target cashier hardware.
