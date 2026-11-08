# Phase 5 — Open Games and Team Challenges

Implemented and verified on 8 November 2026. Final workflow result: **674 assertions passed, 0 failed**. Debug and Release solution builds: **0 warnings, 0 errors**. Phase 6 was not started.

## Implemented behavior

Active, approved owners create games on their own eligible fields. The server derives organizer identity, validates the schedule and capacity, issues a protected creation quote, then commits under the existing SQL field-row gate. Replaying the quote returns the retained game, including after cancellation, rather than inserting another game.

Active players join directly. There are no pending participation requests, owner approvals or simulated payments. A transaction checks eligibility and capacity, then inserts or reactivates the unique `(game_id,user_id)` participant. Repeated joins are idempotent. Counts come from Joined rows in SQL. Leaving changes the row to Left, retaining history and releasing the place.

Owners can view current joined players, change title/level/capacity and cancel upcoming games. Capacity cannot fall below current participation. Field, start and duration remain fixed after creation; cancel and recreate to reschedule. This preserves participant history and durable creation replay. Public responses contain counts, not participant names or account details.

Current SQL team captains can challenge another compatible team, view sent/received proposals, accept/reject incoming proposals and withdraw outgoing pending proposals. Ordinary members and players without a team cannot act as captains. Team locks and fresh captain checks make leadership transfers effective immediately.

Pending proposals do not reserve a field. Acceptance without a field produces an agreement explicitly labelled **No field reservation**. Acceptance with a field rechecks availability, eligibility, format and SQL price, then creates one real Confirmed booking and two `booking_teams` historical snapshots atomically. No payment, booking share, commission or settlement is created. A changed price requires a new proposal.

The existing `.html` routes, navigation, hero designs, typography, colors and responsive shell are retained. SQL lists, details and dialogs replace prototype controls on the migrated pages. Dashboard game navigation, totals, recent activity and homepage game highlights use SQL. Shared prototype files remain available to unfinished modules.

## Schedule, lifecycle and authorization

- Amman time: UTC+03:00, consistently with Phase 3. Exactly 60 or 120 minutes, full-hour starts, opening 13:00 and closing 23:00. Two-hour sessions cannot start at 22:00. Past sessions are rejected on the server.
- Game participation leaving cutoff: **at least one hour before start**, including the exact boundary. This is separate from the unchanged **three-hour player cancellation cutoff** for individual field bookings.
- Games retain SQL statuses Scheduled/Cancelled. Open, Full, In progress and Completed are derived from current SQL participation and time; clients cannot assign them.
- Challenges reuse Open/Agreed/Booked/Cancelled and request Pending/Accepted/Declined/Withdrawn. The UI presents Pending, Accepted, Rejected, Cancelled or Expired. Responding twice or using stale state returns 409. Expiry is derived; history is not deleted.
- Booking Pending/Confirmed, noncancelled Open Games and existing noncancelled tournament occupancy block the field. Challenges block only through an actual blocking booking. `[start,end)` permits adjacent sessions.
- Identity role authorization, Active-account enforcement and global antiforgery validation remain in place. Submitted actor IDs, statuses, prices and participant counts are ignored. Private challenges require current host/target captain authority. Owners cannot manage another owner's game or field.

## Shared occupancy and concurrency

`FieldOccupancy` centralizes the existing occupancy predicates and SQL `UPDLOCK,HOLDLOCK` field-row gate. Booking creation/status updates, field management and Phase 5 scheduling agree on this gate. All reservation writers recheck conflicts inside their transaction. Game joins, leaves, edits and cancellation use the same field gate, preventing over-capacity and lifecycle races.

Challenges lock team rows in ascending ID order, re-read captaincy and status, then acquire the field gate when a reservation is needed. Captains and historical team names are frozen in `booking_teams` at acceptance. Protected versions reject stale responses. Protected game creation quotes bind the owner and session and expire after 15 minutes; retained matching creation replay is checked before expiry.

Executed real concurrent SQL/HTTP scenarios: final-slot joins, repeated joins, leave versus join, two-owner scheduling attempts, booking versus game, booking versus challenge reservation, game versus challenge reservation, accept versus reject, captain transfer versus response, and cancellation versus participation. SQL invariants were checked after the races. All scenarios passed; an unauthorized second owner never acquired authority over the first owner's field.

## Database integration and migration

Reused `open_games`, `game_participants`, `challenges`, `challenge_requests`, `teams`, `team_members`, `fields`, `users`, Identity role tables, `bookings` and `booking_teams`. No new tables, foreign keys or indexes.

The challenges table lacked a duration snapshot. The only migration is `20261108164731_ChallengeDurationPhase5`:

1. Add `dbo.challenges.duration_minutes int NOT NULL DEFAULT 60`.
2. Add trusted/enabled `CK_challenges_duration_minutes`, accepting only 60/120.
3. Add the corresponding EF migration-history row.

No computed columns, pre-existing constraints, FKs, indexes, Identity definitions or other schema objects were changed. Model/schema checks passed; EF reports no pending model changes and all four migrations applied to Kicks. The historical SQL design export remains unchanged; offline parity tests explicitly account for this approved addition.

Rehearsal on the restored database included two temporary legacy challenges, a successful upgrade with all original column values preserved, refusal to downgrade while a two-hour challenge existed, safe downgrade with only one-hour data, and re-upgrade. The fixtures were removed afterwards. The original database had no pre-existing challenges; the legacy rehearsal deliberately tested the nonempty case.

## Backup, rehearsal and preservation

Fresh backup, made for this phase:

`C:\Users\USER\Desktop\KicksMvc-safety\phase5-20261108_1717426b\Kicks-fresh-phase5.bak`

SHA-256:

`116B961C85989AB99E11FCEA05F49B52B29FBC0B3F808195D340E65DB2286EEF`

COPY_ONLY, CHECKSUM, compression, RESTORE VERIFYONLY with CHECKSUM, full isolated restore and DBCC CHECKDB all passed. The fresh external folder grants the current Windows user access and the SQL Server service the access required to create the backup; system-wide ACLs were not weakened. The original backup was not overwritten. App_Data, local configuration and a verified Git bundle were saved alongside it.

All mutating tests used **Kicks_BookingTest_20261108_1717426b** on localhost, with separate physical files. Existing C#, PowerShell and JavaScript gates reject missing targets, the original Kicks database, unrecognized database names and unsafe application ports. No test used a real administrator password or created a real administrator.

Before the original migration, canonical SHA-256 fingerprints matched every original table and column, and the full original schema matched its initial snapshot. After migration, all pre-existing business/account rows and original columns still matched. All original tables, FKs and indexes matched. Only the reviewed column/check and migration-history addition differed.

Final original inventory: **35 physical tables, 76 foreign keys**. Total rows changed from 16 to 17 solely because of the migration-history entry. Real Administrator and approved Player records, password hashes, security stamps, roles, approvals and identity counters remained unchanged. Existing reference amenities and all pre-existing field/booking/team records were preserved. No accidental test data remained. All isolated test-created records and files were cleaned up; original cloned records remained intact.

All **4,925 original frontend files** retained identical hashes. MVC App_Data files and development configuration retained their original inventories/hashes. DBCC CHECKDB passed on the restored database and the original database. The original database was never dropped, reset, reseeded or restored over.

## Executed tests

| Suite | Passed | Failed |
| --- | ---: | ---: |
| Phase 1 SQL authentication | 84 | 0 |
| Phase 1 Edge authentication/review | 56 | 0 |
| Phase 2 SQL fields | 52 | 0 |
| Phase 2 Edge fields | 33 | 0 |
| Phase 3 SQL bookings | 111 | 0 |
| Phase 3 Edge bookings | 46 | 0 |
| Phase 4 SQL teams | 91 | 0 |
| Phase 4 Edge teams | 30 | 0 |
| Database isolation | 15 | 0 |
| Phase 5 SQL | 109 | 0 |
| Phase 5 Edge | 47 | 0 |
| **Total** | **674** | **0** |

The existing baseline was rerun, not assumed: 518 passed. Phase 5 SQL contains 52 Open Games assertions, 30 Challenges assertions, 16 concurrency assertions, plus 11 fixture authentication/account/history/cleanup assertions. Repeated concurrent joins are included in the Open Games section as well. These are assertion counts, not a count of independent race scenarios.

Additional checks, not added to the 674 workflow assertion count: full Debug and Release solution builds, offline model/design parity and 22 behavior checks, pending-model inspection, migration rehearsal/downgrade guard, backup/Git-bundle verification, full preservation audits and seven read-only normal-application route checks. Builds finished with 0 compiler warnings/errors. EF retains the pre-existing runtime warning about the Setting singleton key's constant default; this phase did not alter that mapping.

Edge tests used actual Identity login and real Razor/JavaScript flows on isolated app hosts. They exercised owner creation and field scoping, 120-minute closing-hour options, direct join/count/refresh, full and stale-slot conflicts, leaving, roster visibility, metadata editing, cancellation, dashboard navigation/totals, SQL homepage highlights, challenge creation/accept/reject/withdraw, fieldless agreement, unauthorized member rejection, `.html` compatibility and unchanged prototype storage. No JavaScript runtime errors. Desktop 1440, tablet 768 and mobile 390 layouts and form widths passed overflow checks and screenshots were inspected.

During development, the initial browser harness checked the asynchronous field selector before it loaded; an explicit dialog-ready wait corrected that test. Stale Join error placement, the disconnected hero image and prototype homepage game highlights were corrected and retested. The final counts above are from successful executed runs, not from intermediate failed attempts.

## Files created and modified

Created:

- `Controllers/ActivitiesApiController.cs` — validated owner/player/captain endpoints and scoped summary/selector reads.
- `Services/Activities/ActivityRules.cs` — schedule, price and protected-state rules.
- `Services/Activities/OpenGameService.cs` — scheduling, direct participation, ownership, history and summaries.
- `Services/Activities/ChallengeService.cs` — SQL captain authorization, lifecycle and atomic reservations.
- `Services/Bookings/FieldOccupancy.cs` — common gate and interval predicate.
- `ViewModels/ActivityViewModels.cs` — dedicated inputs; no client-controlled actor/status/price.
- `Views/Shared/_Activities.cshtml`, `wwwroot/css/server-activities.css`, `wwwroot/js/server-activities.js` — responsive SQL UI.
- `Data/Migrations/20261108164731_ChallengeDurationPhase5.cs` and its Designer — minimal additive migration and protected downgrade.
- `tests/Auth.Integration/Phase5Tests.cs`, `tests/activities-browser.cjs` — isolated SQL and Edge coverage.
- This completion report.

Modified:

- `Models/Entities/Challenge.cs`, `Data/KicksDbContext.cs`, model snapshot — duration mapping/default/check.
- `Program.cs` — register Phase 5 services; no automatic migrations or account creation.
- `Services/Bookings/BookingService.cs` — delegate unchanged occupancy semantics to the shared gate/predicate.
- Open Games Index/Details and Challenges Index Razor views — SQL partial, transport, styles; retire only their obsolete prototype dialogs/script wiring.
- Dashboard Owner/Player views and `dashboard.js`/`dashboard-overview.js` — SQL game navigation, summaries/recent activity; administrator game browsing is read-only.
- Home Index and `opendesign-home.js` — SQL game highlights using existing row styles; unrelated homepage sections preserved.
- Test runner Program — Phase 5 suite and fixture modes behind the existing fail-closed gate.
- `tests/ef-model-preparation-check.cjs` — verify the approved additive duration mapping.
- `tests/teams-browser.cjs` — optional evidence directory override; test behavior unchanged.
- `README.md` — document current backend scope.

## Evidence and repeatability

Local evidence: `.local/phase5/` (ignored by Git; contains private database fingerprints and environment-specific paths). Key files: `safety.json`, `original-before.json`, `restored-before.json`, `rehearsal.json`, `migration.sql`, migration/rehearsal logs, `integration.log`, `browser.log`, `browser/*.png`, all four prior-phase SQL/browser logs and screenshots, `isolation.log`, `model.log`, `ef-model.log`, both build logs, `audit-before.json`, `audit-after.json`, identity-counter snapshot, frontend hashes, Git-bundle verification and `normal-readonly-smoke.json`.

For repeat tests, explicitly set `KICKS_TEST_CONNECTION` to a verified restored/migrated database matching the guarded name pattern. Never set it to Kicks. Build the solution before starting test hosts, then run the test DLL with `--phase5`, `--fields`, `--bookings`, `--teams`, or no flag for authentication. Run `tests/activities-browser.cjs`, `field-browser.cjs`, `booking-browser.cjs`, `teams-browser.cjs` with Node. Authentication browser setup/isolated host instructions and scripts are retained under the evidence directory. Test hosts must finish before rebuilding locked binaries. Do not run suites with global snapshot assertions concurrently with other mutating suites.

## Recovery and Git

Branch: **feature/open-games-challenges**, created from verified Phase 4 commit **efd8a2c831833f46653acc9d1b7dfecb21e9378d**. Earlier commits, main and remote refs were preserved. The completed implementation/report are committed locally; the exact resulting commit is recorded in `.local/phase5/verification-summary.json` and reported to the user. No push, merge, reset or history rewrite.

The external `phase4-checkpoint.bundle` passed `git bundle verify` and retains complete prior history. For recovery, stop feature writers first and preserve any data created since the backup. Prefer a reviewed forward fix. An EF downgrade is safe only when every retained challenge has duration 60; Down intentionally refuses to discard any 120-minute value. That refusal and a safe one-hour Down/Up cycle were rehearsed. Full recovery should first restore the verified backup into another database and validate it; replacing the original would require an explicit recovery decision and handling newer records. No automatic restore-over-original or destructive rollback was executed.

## Boundaries and final status

**Phase 5 is completed and verified for the requested scope.** Existing individual-booking prices, protected quotes, history, status transitions, 60/120-minute rules and three-hour cancellation policy remain unchanged.

Game schedule changes require cancellation/recreation; only permitted metadata/capacity edits are supported. Participation history retains the existing Joined/Left row, not an added event ledger. Accepted challenges support the requested accept/reject/pending-withdraw lifecycle; there is no new accepted-team-booking cancellation/refund workflow. Existing individual-booking endpoints continue to protect team/split reservations from inappropriate individual transitions. Historical captain access follows current team captaincy; booking snapshots preserve the captain/team names at reservation time.

There are no payments, split payments, paid statuses, messaging, tournament backend, artificial booking fees or Phase 6 changes. Existing tournament occupancy remains respected without implementing tournament scheduling. Deployment across multiple independent app instances would additionally require a shared persistent ASP.NET Data Protection key ring for portable protected quotes; the current local application's established key configuration is preserved.
