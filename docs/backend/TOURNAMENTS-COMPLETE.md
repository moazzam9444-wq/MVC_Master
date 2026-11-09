# Phase 6 — SQL-backed Tournament Management

Completed and verified on **9 November 2026**, for the supported single-elimination format permitted by the Phase 6 request. **872 workflow assertions passed, 0 failed.** Full Debug and Release solution builds passed with **0 compiler warnings and 0 errors**. No Phase 7, payment integration, hosting deployment, push or merge.

## Inspection and implementation choices

The verified starting checkout was clean on `feature/open-games-challenges`, commit `38408db29bcc67ec2cefdfea17ce20781071a90a`, also recorded by its origin tracking ref. The new branch was created from that commit, not main: **feature/tournaments-management**.

The actual SQL schema already contained tournaments, registrations, roster snapshots, draw entries, matches and source/winner slots. It had uniqueness, historical captain/team snapshots and cross-tournament FK protections, but lacked registration windows, match dates/durations, scores and result states. Existing tournament `ends_at` is computed from its overall window duration; it was preserved. No group/round-robin structure exists. The short plan was given before structural changes: reuse those tables, implement fully correct single elimination, add only missing fields, and use the shared field-row gate for actual scheduled matches.

**Supported:** single elimination, 2–64 configured teams, a minimum of two actual entries, non-power-of-two draws with automatic byes, quarterfinals/semifinals/final as applicable. **Unsupported:** group stage plus knockout. That format would need additional group/fixture structure and is explicitly rejected by new creation inputs; its group-specific tests are not claimed as executed successes.

## Tournament creation and management

Only an authenticated, approved, Active owner creates tournaments on an eligible field that they own. Server inputs include name, description, registration opening/closing, event start/end, capacity, 60/120-minute match duration, fee, prize and level. Actor identity and status are never accepted from the browser.

Creation uses a protected, owner-bound review quote with a unique creation key, 15-minute validity and a filtered unique SQL index. Repeated confirmation returns the retained tournament, including after cancellation, rather than creating duplicates. The fee is validated for range, nonnegative value and two-decimal precision. Registration dates are minute-precision Amman wall times; event and match times use full hours. UTC/timezone-suffixed wall-time inputs and invalid date boundaries are rejected safely.

The tournament remains on its original field. Before any entry, its owner may change event/registration settings, capacity, match duration and fee. After the first entry, including a withdrawn historical entry, those settings are frozen; title, description, prize and level remain editable until cancellation/completion. This preserves the price and event contract without duplicating fee snapshots. Metadata edits, cancellation and draw finalization use protected aggregate versions.

Creating an event **does not reserve its overall field window**. Only actual match schedules reserve time. Event windows can span days; the intervals between matches remain available to normal bookings and games.

## Immediate team registration

Only the current Active player captain may register their own SQL team. Captain authority is re-read under the team row lock used by Phase 4 transfers. Format must match the venue. The roster must contain Active players, and a frozen roster player cannot be entered in another active team entry in the same tournament.

Registration is **direct and immediate**: the row is Registered, never Pending or awaiting owner approval. No approval endpoint or queue was added. Capacity is checked inside the protected SQL transaction; existing unique tournament/team and tournament/captain indexes remain intact. Concurrent final-slot requests allow one winner. Simultaneous repeated registration produces one retained entry, with idempotent replay.

The first entry freezes team name, original captain identity/name/email and roster snapshots using the existing registration tables. Public responses omit private captain/email/account snapshots. Current captains, including successors after transfer, control registration/withdrawal; historical snapshots do not grant authority to a former captain.

Withdrawal is allowed while registration is open and before draw lock. It changes the retained row to Withdrawn, keeps roster/history and withdrawal audit, and releases capacity. Rejoining reactivates the same entry, preserving its first snapshots and last withdrawal audit, with renewed eligibility/conflict checks. Archived fields do not remove a captain's otherwise valid withdrawal control. The existing unique historical captain index also limits a captain to one sponsored entry per tournament.

## Fees and financial integrity

The authorized owner may configure an entry fee; the captain's submitted price/status fields are ignored. The displayed and stored tournament fee becomes fixed after the first entry. A positive-fee entry explicitly says **Registered — Payment Not Processed**. A zero-fee event is Free entry.

No payment record, fake paid status, commission, payout, settlement, refund or split payment is created or changed. Tournament match occupancy does not fabricate paid field-booking records.

## Draw, fixtures, results and standings

The owner may lock the draw when capacity is full or registration has closed, with at least two eligible entries and before event start. Registration and withdrawal then close permanently for that draw. Entries are ordered by original SQL creation time and ID; the existing bracket's alternating-half seeding algorithm distributes them into the smallest covering power of two. There is no random shuffle.

Draw entries, rounds, fixtures and source slots are saved atomically in the existing tables. Empty first-round positions advance by automatic bye without fake scores, played matches or field reservations. Duplicate generation is rejected. Later-round teams are populated from finalized feeder winners, not chosen by the browser.

Only the owner schedules matches. Both participants must be resolved; earlier feeder matches must be Completed. Every match uses the tournament's field and fixed match duration, stays inside the event window, and uses full-hour **13:00–23:00 Amman UTC+03:00** intervals of **60 or 120 minutes**. Past starts, other fields, conflicts and simultaneous tournament matches for the same team on different fields are rejected.

Scores can be finalized only after a scheduled match ends. Home/away goals must be integers 0–99. A tied knockout score requires two unequal shootout scores, also 0–99; shootout goals remain separate from match goals. Otherwise penalty fields must be empty. Winner identity is derived server-side from the actual participating slots and scores. UpdatedBy/UpdatedAt record the result author and time.

Finalization and advancement commit in one transaction. Completed results are immutable, including before later rounds are scheduled; duplicate/stale finalization and correction attempts return conflict. This is the documented correction policy rather than silently clearing downstream results. A valid completed final persists the champion in its existing `winner_registration_id` FK. Completion/champion display derives from that durable final, without a second winner store.

Standings are rebuilt server-side from **persisted SQL entries, slots and completed scores**. There is no browser authority or independent mutable standings cache/table. They survive refresh/fresh contexts through those durable normalized facts. Records include played, wins, draws, losses, goals for/against/difference and points. A decisive knockout winner, including a shootout winner, receives a win; byes are excluded. Presentation orders the actual champion first, then wins, goal difference, goals for and registration ID. These are knockout tournament records, not an implemented group league; group draw scoring/advancement is outside the supported format.

## Shared field occupancy and concurrency

`FieldOccupancy` remains the sole shared interval predicate. It includes existing Pending/Confirmed bookings, active Open Games, challenge reservations through actual blocking bookings, and Scheduled/Completed tournament match intervals. `[start,end)` permits adjacency. A match has one schedule row; no duplicate booking/occupancy record is created.

New tournaments are identified by their explicit match duration and never block their overall event window. Pre-Phase-6 rows with no match configuration remain read-only and conservatively retain their previous window occupancy, protecting unknown legacy reservations. The original database had zero such tournament rows; this compatibility branch was exercised by the existing booking regressions and nonempty migration rehearsal.

All tournament mutations acquire the existing SQL field-row `UPDLOCK,HOLDLOCK` gate **before** their tournament row gate. Operations involving teams first lock relevant team rows in ascending order, then field, then tournament. Scheduling rechecks occupancy and overlapping team fixtures inside this transaction. The pre-lock participant identity is checked again under the protected version, so stale resolved teams cannot bypass their locks. Existing booking, game and challenge writers consume this same predicate and gate.

Cancelling an unplayed future match retains its dates/history and releases occupancy by changing status. Rescheduling updates that same fixture safely. Completed matches cannot be rescheduled or cancelled. Tournament cancellation retains completed history and cancels remaining draft/future fixtures, releasing their time; unrelated bookings are untouched. An ongoing match must end before the remaining tournament can be cancelled. A tournament with a completed final remains read-only.

Executed real concurrent HTTP/SQL scenarios: final-slot registration, simultaneous duplicate registration, registration versus withdrawal, cancellation versus registration, two tournament scheduling attempts, booking versus tournament scheduling, game versus tournament scheduling, challenge acceptance versus tournament scheduling, match cancellation versus booking confirmation, captain transfer versus registration, simultaneous score finalization, and attempted result correction versus downstream advancement/scheduling. SQL capacity, uniqueness, occupancy or advancement invariants were checked after these races. Sequential checks additionally verified both occupancy directions and cross-field team conflicts.

## Frontend, dashboard and authorization

Existing tournament `.html` routes, header/footer, tournament hero, card classes, colors, typography, field photographs and dashboard shells were retained. New SQL forms and API transport replace only tournament prototype wiring on those pages; old shared prototype files remain for unfinished modules.

Owner UI: My Tournaments, create/edit/cancel, registered teams/capacity, lock draw, schedule/reschedule/cancel matches, enter scores/shootouts, standings, bracket and champion. Captain UI: discovery/search, direct registration, registered/payment-unprocessed state, withdrawal, team tournament history and fixtures. Public/player UI shows SQL schedules, registered team names, results, bracket and champion. Administrator tournament navigation is read-only; capability flags also respect the current role when actor IDs previously owned fields or captained teams.

Homepage tournament highlights and all dashboard tournament/entry navigation, totals, charts and recent activity consume SQL. Tournament/registration localStorage is not read as authoritative persistence and is not written by migrated flows. The existing Phase 3 three-hour player cancellation rule, pricing/quotes and Phase 1–5 actions were preserved.

Every mutation retains Identity role/Active-account enforcement, resource ownership/current captain checks, server input validation, antiforgery protection, protected stale-state tokens and safe conflict responses. No real administrator password was requested or used. Read-only administrative tests used the existing isolated test-host principal; production authentication does not register that handler.

## Tables and exact schema changes

Reused `tournaments`, `tournament_registrations`, `registration_players`, `tournament_draw_entries`, `tournament_matches` and `tournament_match_slots`, with the existing fields, teams, memberships, users, role and occupancy relationships. **No new tables or foreign keys. No existing columns, computed expressions, FKs or indexes were replaced or removed.**

Migration: **20261109070739_TournamentManagementPhase6**.

| Table | Additions |
| --- | --- |
| tournaments | nullable creation_key, description, match_duration_minutes, registration_opens_at, registration_closes_at |
| tournament_matches | nullable starts_at, duration_minutes, computed persisted ends_at, status, home_score, away_score, home_penalties, away_penalties |
| indexes | filtered unique UQ_tournaments_creation_key; filtered IX_tournament_matches_schedule |
| constraints | six new checks for configuration/windows, match status, duration, operating schedule, score bounds and completed result consistency |

All 13 columns are nullable for unchanged legacy rows. The new computed end is on tournament_matches; existing tournament/bookings/game computed ends are preserved. Down refuses to drop any populated Phase 6 fields, including descriptions, schedules or scores, rather than discard retained history. Startup still never applies migrations or seeds accounts/data automatically.

## Fresh backup, rehearsal and preservation

Backup: `C:\Users\USER\Desktop\KicksMvc-safety\phase6-20261109_75325cec\Kicks-fresh-phase6.bak`

SHA-256: `6708C7390F4169D31BFCCFA10B8D6430F45E83868F23850707E8D4AC10C9B8F9`

Fresh COPY_ONLY, CHECKSUM, compressed backup; RESTORE VERIFYONLY with checksum; separate restore; initial and final DBCC CHECKDB: **passed**. The new external safety folder grants only the current Windows user and required SQL service access; no system-wide security relaxation or overwrite of an earlier backup. App_Data, local development configuration and a complete Phase 5 Git bundle were saved alongside it. `git bundle verify` passed.

Isolated database: **Kicks_BookingTest_20261109_75325cec**, with independent physical data/log files. Every mutating suite/fixture used the existing fail-closed C#, PowerShell and JavaScript gates and explicit KICKS_TEST_CONNECTION. They reject Kicks, missing/unrecognized targets and unsafe/occupied normal application ports.

The migration SQL was reviewed before use. Rehearsal seeded a clearly marked nonempty legacy tournament, field/team, registration, draw and recorded bye winner in the restored database. All pre-existing column values, 76 FKs and pre-existing indexes matched after upgrade. A destructive downgrade with populated Phase 6 data was correctly refused; safe downgrade with only null additions and re-upgrade passed. Only those isolated rehearsal fixtures were removed.

Only after builds, all suites, rehearsal and pre-upgrade preservation checks passed was the migration applied to original Kicks. The post-upgrade audit verified:

- **35 physical tables and 76 FKs**, unchanged table inventory and original relationships.
- Every original business/account row and original column fingerprint unchanged, including Identity hashes, stamps, roles, approvals, fields, images, bookings, teams, captain assignments, games, participants, challenges and tournament history.
- Original identity counters unchanged; no reseeding or resetting anywhere.
- Original four migration rows unchanged; the fifth row is the reviewed Phase 6 addition.
- Original total row count increased **17 → 18 only through migration history**. No test fixtures or sample tournament records in original Kicks. Original had two real accounts and no tournaments at inspection.
- Exactly the reviewed 13 columns, two indexes and six trusted/enabled checks added; existing schema metadata/indexes preserved.
- All **4,925 original static frontend files**, MVC App_Data inventory/content and development configuration hashes unchanged.
- Isolated fixtures cleaned; original cloned records preserved. DBCC CHECKDB passed on both databases after testing/upgrade.

Eleven read-only routes/APIs also returned 200 on the normal local application using Kicks after migration. The temporary verification process was stopped so Visual Studio can use port 5602. No hosting deployment or database restore-over-original occurred.

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
| Phase 5 SQL games/challenges | 109 | 0 |
| Phase 5 Edge games/challenges | 47 | 0 |
| Database isolation | 15 | 0 |
| **Phase 6 SQL tournaments** | **153** | **0** |
| **Phase 6 Edge tournaments** | **45** | **0** |
| **Total** | **872** | **0** |

The historical 674 baseline was actually rerun. Phase 6 SQL sections: fixture authentication 14; creation/oversight 31; direct registration 21; fixtures/scheduling 22; results/standings/champion 17; cross-module/concurrency 19; additional scheduling/security/history 24; financial/preservation 4; cleanup 1. The assertion total does not label sequential checks as race tests and does not count unsupported group-stage features as passed.

Additional checks outside that workflow assertion total: full Debug/Release builds (0 warnings/errors), no pending EF model changes, offline SQL/model parity plus 22 behavior checks, raw schema/fingerprint audits, backup/restore/checksum/DBCC, Git-bundle verification, migration rollback rehearsal and 11 normal-app read-only smoke routes. The pre-existing EF runtime diagnostic about the Setting singleton key's constant default remains unchanged; it is not a compiler warning introduced by this phase.

Edge used actual MVC/Identity/Razor/JavaScript flows with isolated accounts and SQL: owner field selection/create/edit, captain discovery/direct register/withdraw/full/stale conflicts, role rejection, homepage/dashboard links, draw, overlapping/adjacent scheduling, score entry, shootouts, standings, bracket, champion/refresh, cancellation and storage preservation. **1440/768/390** directory, form and bracket/standings layouts passed overflow checks; screenshots were inspected. Brackets/standings scroll inside their own regions on smaller screens. No JavaScript runtime errors.

Browser result tests used an explicit guarded fixture command to age only that run's already-scheduled test matches into nonoverlapping historical intervals, avoiding a real-hour wait. Creation, scheduling, result entry and advancement still used the real UI/backend. This fixture command exists only in the test executable; production has no clock-shift endpoint or bypass. SQL tests separately exercised time guards using an injected test TimeProvider.

During development, the first browser run exposed an outdated default scheduling date after event start; it was corrected and rerun. Range review added controlled rejection of extreme/UTC-suffixed dates and renewed frozen-roster eligibility. Final review corrected role-specific read-only capability flags and withdrawal visibility for archived fields, with SQL regression assertions. A test initially expected 403 for administrator role denial; the established cookie architecture returns an access-denied redirect, so the assertion now verifies that preserved behavior. Final results above exclude intermediate failed development attempts; the role-response diagnostic log is retained locally.

## Files created and modified

Created:

- `Controllers/TournamentApiController.cs`.
- `ViewModels/TournamentViewModels.cs`.
- `Services/Tournaments/TournamentService.cs`, `TournamentRegistrationService.cs`, `TournamentMatchService.cs`, `TournamentReadService.cs`.
- `Views/Shared/_Tournaments.cshtml`, `wwwroot/css/server-tournaments.css`, `wwwroot/js/server-tournaments.js`.
- Migration `20261109070739_TournamentManagementPhase6.cs` and Designer.
- `tests/Auth.Integration/TournamentTests.cs`, `tests/tournaments-browser.cjs`.
- This completion report.

Modified:

- Tournament/TournamentMatche entities, KicksDbContext and model snapshot: approved additive mapping only.
- Program.cs: register tournament service; no startup migrations/test authentication.
- FieldOccupancy: canonical scheduled match occupancy and retained legacy compatibility.
- Tournament Index/Details Razor: SQL partial and transport; retire only tournament prototype scripts/dialog wiring.
- Dashboard Admin/Owner/Player and dashboard JS/overview: SQL tournament navigation, summaries/recent activity/charts; admin remains read-only.
- Home Index/opendesign-home.js: SQL tournament highlight using existing row design.
- Integration runner: guarded tournament suite and isolated setup/cleanup/history-age fixture modes.
- EF model parity test: verify nullable additions, computed end, constraints and indexes without weakening the historical baseline checks.
- README: current completed scope and report link.

## Evidence, recovery and final status

Evidence is under **.local/phase6/**, ignored by Git: safety.json, original/restored/rehearsal/final fingerprints, frontend/config/counter hashes, reviewed migration.sql, rehearsal/down/re-up/original migration logs, all SQL/browser logs, browser screenshots, both build logs, EF checks, isolation results, preservation audits, normal read-only smoke and verification-summary.json. Sensitive/environment-specific artifacts remain outside source control.

Branch: **feature/tournaments-management**. Base/checkpoint: **38408db29bcc67ec2cefdfea17ce20781071a90a**. The completed implementation/report are committed locally; the exact resulting commit is recorded in `.local/phase6/verification-summary.json` and the final user report. Previous branches, main, remote refs and history are preserved. No push, merge, reset or history rewrite.

For recovery, stop feature writers and preserve any records created since the backup. Prefer a reviewed forward fix. EF Down is deliberately blocked if populated Phase 6 data would be lost. Safe null-only Down/Up was rehearsed. Full recovery should first restore the verified backup into another isolated database and validate it; replacing the original or losing newer records requires a separate explicit recovery decision. Do not resume old tournament prototype workflows against new SQL history. No destructive rollback was executed on Kicks.

**Phase 6 is complete and verified for its supported single-elimination scope.** Limitations: no group stage, no result correction after finalization, no rescheduling of completed matches, no field change after creation, no core event/fee/roster editing after first entry, and later-round scheduling waits for resolved feeder teams. Registration snapshots and last withdrawal audit are retained rather than a new event ledger. Ongoing matches must end before cancelling the remaining tournament. Legacy history is read-only with conservative prior occupancy. Fees are informational/unprocessed; payments, payouts and refunds remain excluded. Multi-instance deployment would need the application's protected quote/version keys shared appropriately; deployment configuration was intentionally outside this phase.
