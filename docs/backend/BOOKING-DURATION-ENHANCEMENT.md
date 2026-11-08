# KICKS — Phase 3 duration and opening-hours enhancement

Completed 8 November 2026 (Asia/Amman), in `C:/Users/USER/Desktop/KicksMvc`.

**Result:** implemented, verified on the restored SQL database, and deployed to the original `Kicks` database using one new EF Core migration. The updated application is running at `http://127.0.0.1:5602`. Phase 4 and payments were not started.

## Booking behavior

- Global opening is **13:00**, closing **23:00**, using the existing Amman **UTC+03:00** contract and server UTC instants for comparisons.
- Durations are exactly **60 or 120 minutes**, with full-hour starts. One-hour starts are 13:00–22:00; two-hour starts are 13:00–21:00. No reservation may end after closing.
- Availability is read from SQL for the selected date and duration. Both hours of a two-hour request must be free. Pending/Confirmed reservations and existing non-cancelled open-game/tournament intervals block availability.
- Server total is SQL hourly price multiplied by duration in hours. Decimal overflow is rejected before insertion. The agreed total is retained in `bookings.price`; later field price edits do not rewrite history.
- Availability returns a server-calculated total, and confirmation obtains a protected quote. V2 quotes bind player, field, full start, duration, total, duration-specific history watermark, and expiry. V1, tampered, and expired unused quotes are rejected. Price, eligibility, time, and occupancy are rechecked at confirmation.
- Repeated confirmation returns the first matching retained result after the signed watermark, scoped to the same duration. It cannot replay a different-duration booking or recreate a cancelled booking. A used quote can return its existing result after expiry; it cannot create a new reservation.
- Confirmation remains independent of payment settlement. No payment, receipt, refund, team, split-payment, or commission-processing workflow was introduced. Commission remains zero and OwnerNet is the unsettled allocation required by the existing arithmetic constraint.

## Concurrency, authorization, and cancellation

Creation and status changes retain the existing transaction and field-row `UPDLOCK,HOLDLOCK` protocol, also used by field edits/archive. Protected confirmation checks the complete half-open interval `[start,end)` under this lock. The filtered unique `(field_id,starts_at)` index remains an additional equal-start guard, not the interval-overlap mechanism.

Three SQL integration rounds raced different players booking **17:00–19:00** and **18:00–19:00** simultaneously. Every round returned one Created and one Conflict, with one blocking SQL row. The pre-existing equal-start race also passed. The browser tested competing quotes with different starts/durations and correctly showed HTTP 409, refreshed availability, and disabled the occupied slot. Adjacency at 19:00 after a session ending at 19:00 remains valid.

Players can cancel only their own Confirmed reservations with **at least three hours before the scheduled start**, independent of duration. Exactly three hours passed; one second inside the cutoff was rejected for both durations. Owners retain future own-field Pending/Confirmed cancellation without that cutoff and may complete only ended Confirmed sessions. A two-hour session was rejected for completion after 90 minutes and accepted exactly at its persisted end. Cancelled rows retain audit history and release the full interval.

Identity roles, Active-account checks, resource scoping, CSRF protection, protected status fingerprints, and rejection of foreign booking IDs remain in place. The duration is part of the booking fingerprint. No real administrator password was requested or used.

## Git checkpoint and source protection

- Working branch: `feature/booking-duration-hours`.
- Preserved Phase 3 checkpoint: `ce7fb0e6bb09ce557d1254bf89732e8a782de10e`.
- Preserved annotated tag: `codex/phase3-checkpoint-20261108`.
- `main` and `origin/main` retain the original `9b90219276ae1dc793773ea08770289e245181b6` commit. No history rewrite, force-push, or push was performed.
- A verified complete Git bundle is at `C:/Users/USER/Desktop/KicksMvc-safety/booking-duration-20261108_cc8e9a2c/phase3-checkpoint.bundle`.
- The previously uncommitted/untracked Phase 3 source was checkpointed before implementation. Sensitive development configuration, `.local`, `App_Data`, runtime output and backups are excluded from source commits. Normal development configuration remains unchanged.

## Backup permission resolution and verification

Original backup, preserved without modification:

`C:/Program Files/Microsoft SQL Server/MSSQL17.MSSQLSERVER/MSSQL/Backup/Kicks_duration_20261108_cc8e9a2c.bak`

Accessible verified copy:

`C:/Users/USER/Desktop/KicksMvc-safety/booking-duration-20261108_cc8e9a2c/Kicks_duration_20261108_cc8e9a2c_verified-copy.bak`

Size: **1,548,288 bytes**. Original and copied SHA-256:

```text
BC3304CC86B70AA1B8CCFD9D19DC6673032C61FD5D1C30118F6059046D44DE5E
```

The copy helper was inspected and syntax-checked. Normal Windows `RunAs` elevation succeeded using the existing PowerShell 7 RemoteSigned configuration. No UAC bypass, execution-policy change, login activation, server-option change, or system-wide permission change was made. The helper opens the source read-only, uses CreateNew for the destination, compares hashes, and verifies the source ACL remains unchanged. The copy has explicit Read permissions only for `DESKTOP-4CBPOU8\USER` and `NT SERVICE\MSSQLSERVER`, with inherited access removed. An independent unelevated SHA-256 calculation matched afterward.

SQL `RESTORE VERIFYONLY WITH CHECKSUM` passed on the copy. A full restore of the copy into `Kicks_BackupVerify_20261108_cc8e9a2c` and `DBCC CHECKDB` passed. Its 35 table contents and schema matched the saved original baseline. The existing restored database `Kicks_BookingTest_20261108_cc8e9a2c` was preserved and used for rehearsal/tests. Both use separate physical MDF/LDF files. Neither restore overwrote the original database or its files.

The original backup was COPY_ONLY with backup checksums. Private application data, local development configuration, and existing Data Protection keys were separately copied into the external safety directory. Backup files/configuration/private assets remain outside Git.

## Test isolation

All C# integration, fixture and review-host entry points require an explicit `KICKS_TEST_CONNECTION`. The shared gate checks local Windows authentication, an allowed restored database name, actual `DB_NAME()`, and separation from the original physical files before host/fixture construction. It configures the connection only in that process.

The PowerShell authentication SQL helper no longer hardcodes `Kicks`; it uses the same fail-closed validation. Browser entry points validate the database before doing fixture work. Authentication browser tests require an explicit isolated URL and reject port 5602. Tests that launch their own applications/review bridge refuse occupied ports, preventing accidental use of an unknown listener.

Fifteen isolation assertions passed, including rejection of missing targets, original Kicks, normal application URLs and occupied test ports, and acceptance of the verified restored target. These negative checks use read-only gates; they do not create fixtures in the original database. The test-only administrator review principal remains confined to the excluded integration-test assembly and protects existing accounts from mutation.

## EF Core migration and rehearsal

New migration: **`20261108072042_BookingDurationAndOpeningHours`**.

Only the following schema changes were approved/applied:

1. Add `bookings.duration_minutes int NOT NULL DEFAULT 60`.
2. Change persisted computed `ends_at` to `DATEADD(MINUTE,[duration_minutes],[starts_at])`.
3. Narrow `CK_bookings_rule_4` to Amman-offset full-hour starts 13:00–22:00.
4. Add exact 60/120 duration and 23:00 closing constraints.
5. Append the one intended EF migration-history row.

The generated SQL was reviewed. It changes no Identity table, field table, key, index, or foreign key. The two earlier applied migrations were neither edited nor replayed. Existing check constraints remain enabled/trusted.

Before DDL, Up locks/checks existing bookings and aborts if reservations conflict with the new schedule. Existing rows are never moved, cancelled, repriced or deleted by migration. The original database still had no booking rows immediately before deployment.

Rehearsal seeded a completed historical and a future one-hour booking in the restored database. Both preserved all pre-existing column values, including IDs, starts, ends, prices, status and relationships; duration became 60. Every unrelated table's values and all 76 foreign keys/indexes were preserved. Only the marked rehearsal fixtures were then removed.

Down first refuses any retained non-60-minute booking, including historical outcomes. The rehearsal verified refusal with a two-hour row, with data/history intact, then removed that marked fixture and successfully performed a safe isolated downgrade and re-upgrade. Dependency ordering was explicitly corrected so `ends_at` stops depending on duration before duration is dropped.

## Original deployment and preservation

Only after rehearsal, all application suites, isolation checks, Release builds and compatibility checks passed was the original application writer stopped. The quiesced original database was compared again to the pre-test baseline before migration. Its data and schema were identical. The reviewed migration was applied once, and the new Release app was started at port 5602.

The final audit established:

- All original table values are unchanged, except the intentional new migration-history row. Booking comparisons use the complete pre-existing column projection so the added duration column cannot conceal historical differences.
- Both real user rows, password hashes, approvals, stamps, role assignments and related Identity data match the original SHA-256 fingerprints. There remains one administrator and one approved player.
- Six reference amenities and all unrelated domain data remain intact. No original fixtures or bookings were inserted/deleted by tests.
- Original identity counters match the untouched restored backup and remain unchanged after deployment, providing additional evidence against test insert/delete activity in Kicks.
- Physical inventory remains **35 tables and 76 foreign keys**. Table/FK object IDs and indexes are unchanged; schema differences are limited to the reviewed booking columns/checks. Old migration entries are unchanged, and exactly one new entry exists.
- All **4,925** original frontend files under `C:/Users/USER/Desktop/Kicks` retain their hashes and inventory.
- Original development configuration is unchanged. Private-file inventory is unchanged (zero files at the captured baseline and completion).
- All test-created accounts, fields, bookings, activity rows, documents and images were cleaned only from the isolated environment. Restored safety databases are deliberately retained; there are no temporary fixture rows left.

Normal-app read-only health checks passed: homepage, catalogue API, catalogue/details HTML routes, updated duration controls and JS return 200; anonymous Player/Owner dashboard requests redirect to login. No real-account login or production booking fixture was used for this check.

## Final executed results

| Suite | Passed | Failed |
|---|---:|---:|
| Booking SQL integration, rules and concurrency | 111 | 0 |
| Booking Edge/Playwright browser workflows | 46 | 0 |
| Authentication SQL integration | 84 | 0 |
| Authentication Edge/Playwright browser regression | 56 | 0 |
| Field-management SQL integration | 52 | 0 |
| Field-management Edge/Playwright browser regression | 33 | 0 |
| Fail-closed isolation assertions | 15 | 0 |
| **Total** | **397** | **0** |

All 225 previous-phase assertions remain. The original booking suite was extended rather than replaced; obsolete hours/counts were updated to the approved rules. Additional backup, migration, rollback, model/schema, preservation and operational health verifications also passed.

Release solution and integration-project builds succeeded with **0 warnings, 0 errors**. EF reports no model changes pending after the new migration. Offline parity checks explicitly account for the additive enhancement while preserving the historical SQL export and unrelated checks. Database documentation validation and Git whitespace checks passed.

Browser tests used actual Microsoft Edge against the MVC application with SQL-backed isolated fixtures. They covered duration switching, authoritative totals, confirmation, refresh/history persistence, duplicate clicks, different-start conflicts, cancellation, owner actions and authorization. Desktop/tablet/mobile widths **1440, 768, 390** had no horizontal overflow or JavaScript runtime errors. Screenshot review found a remaining hardcoded ONE HOUR confirmation-card label; it was corrected and the affected full booking browser suite passed again, including a new assertion for that label.

## Files created and modified

Created:

- `Data/Migrations/20261108072042_BookingDurationAndOpeningHours.cs` and its generated `.Designer.cs`.
- `tests/Auth.Integration/TestDatabase.cs`.
- `tests/require-test-database.ps1`, `tests/test-database.cjs`.
- This report.

Modified:

- `Models/Entities/Booking.cs`, `Data/KicksDbContext.cs`, `Data/Migrations/KicksDbContextModelSnapshot.cs`.
- `Services/Bookings/BookingService.cs`, `Controllers/BookingsController.cs`, `ViewModels/BookingViewModels.cs`.
- `Views/Fields/Details.cshtml`, `Views/Shared/_Bookings.cshtml`, `Views/Dashboard/Player.cshtml`, `Views/Dashboard/Owner.cshtml`.
- `wwwroot/js/server-bookings.js`.
- `tests/Auth.Integration/Program.cs`, `tests/Auth.Integration/BookingTests.cs`, `tests/ef-model-preparation-check.cjs`.
- `tests/auth-browser-data.ps1`, `tests/auth-browser.cjs`, `tests/auth-final-browser.cjs`, `tests/booking-browser.cjs`, `tests/field-browser.cjs`.
- `.gitignore`, `README.md`.

Safety helpers, migration SQL, logs, hashes, snapshots, audit JSON and screenshots are saved under `.local/booking-enhancement/` and excluded from Git. Authentication implementation/configuration, field-management services, prior migrations, shared unfinished prototype modules and original frontend source were not changed.

## Reproduction and remaining limits

For isolated tests, set the process-only connection to the retained restored database:

```powershell
$env:KICKS_TEST_CONNECTION='Server=localhost;Database=Kicks_BookingTest_20261108_cc8e9a2c;Trusted_Connection=True;TrustServerCertificate=True;'
dotnet run --project tests/Auth.Integration/Auth.Integration.csproj -c Release -- --bookings
dotnet run --project tests/Auth.Integration/Auth.Integration.csproj -c Release
dotnet run --project tests/Auth.Integration/Auth.Integration.csproj -c Release -- --fields
node tests/booking-browser.cjs
node tests/field-browser.cjs
```

Run fixture suites sequentially. The authentication final-browser suite additionally needs an explicitly started isolated Development app using that same process connection, `KICKS_TEST_URL` on a free non-5602 loopback port, and its copied-account baseline evidence. The executed orchestration/evidence is preserved in `.local/booking-enhancement/`.

Quotes do not reserve inventory until confirmation. Opening hours are global; there are no per-field calendars, arbitrary durations, half-hour starts or rescheduling. Future SQL writers for games/tournaments must use the same field-row locking protocol. Existing team/split/challenge mutations remain outside this workflow.

Do not deploy the previous one-hour application or blindly downgrade after a two-hour booking exists. Prefer a compatible forward fix. Restoring the pre-enhancement backup over a database that has accepted subsequent writes would lose those writes and requires a separate recovery decision. No payment processing or Phase 4 work is included.
