# KICKS — Phase 4 Teams Management completion

Implemented in `C:/Users/USER/Desktop/KicksMvc`, from the completed Phase 3 commit `7e2219a98535e6c1856957eb0283710dd935be3d`, on branch **`codex/teams-management`**. No push, merge, reset, or history rewrite was performed. The updated local app is running at `http://127.0.0.1:5602`.

## Features and existing design

The existing `/Pages/Teams/teams.html` route and its KICKS navigation, hero, cards, forms, dialogs, colours and typography are retained. Its Discover teams, My team and My requests tabs now query SQL. Team profiles remain addressable with `?team=<SQL id>`, including after refresh. The Player Dashboard's existing Teams link reaches this page.

Active Identity players can create a team, browse/search/filter the directory, view profiles/rosters, send and cancel join requests, inspect request history, and leave an ordinary membership. Creation atomically saves the team and captain membership, deriving captain ID from Identity. Description, area, matchday and logo are optional; existing city, level, format, capacity, initials, colours and recruiting-position fields are supported.

Captains can edit their own team, change recruitment/capacity, accept/reject requests, remove ordinary members, and transfer captaincy to an existing Active player member. The former captain remains an ordinary member and loses captain permissions immediately. A captain cannot leave or remove themselves before transferring leadership. No team-deletion endpoint was added.

## Schema reused — no migration

The inspected schema already provides everything required:

- `teams`: captain, unique name, city/area, level, format, capacity, biography, matchday, initials, badge URL, colour, recruiting flag and creation timestamp.
- `team_members`: current membership, position and join timestamp. Its `user_id` primary key enforces the existing **one team per player** rule; the existing prototype UI stated the same rule.
- `team_requests`: join/leave type, position/message, Pending/Accepted/Declined/Cancelled status and reviewer/time audit.
- `team_recruiting_positions`: the existing recruiting-position collection.
- Existing users/Identity roles, unique captain/name indexes, filtered unique pending-request index, and NoAction foreign keys.

**No tables, columns, constraints, indexes, entities, EF mappings, migration files or migration-history entries were changed.** Physical inventory remains 35 tables and 76 foreign keys; the existing three applied migrations remain unchanged. EF reports no pending model changes.

SQL `Declined` is displayed as **Rejected** in the API/UI to provide the requested lifecycle without replacing the existing database enum. Accepting a request inserts one membership; rejecting it inserts none. Accepted/rejected/cancelled request rows remain as history. Departures/removals remove the current-roster row and append an audited Accepted leave event; previous join records and match relationships remain intact. Existing pending leave requests remain supported by the captain review endpoint.

## Transactions, concurrency and history

Team mutations use EF transactions and `UPDLOCK,HOLDLOCK` on the team row. Membership-changing operations also gate on the relevant SQL user row, serializing competing admissions/creation across different teams. The existing SQL uniqueness constraints provide a second guard. Capacity is checked again under the team lock. Team creation rolls back both the team and membership if either cannot be saved.

Team edits/removal/departure/transfer carry protected state versions. Request decisions carry protected request versions. Bad or stale tokens produce safe conflicts. A request is reloaded after obtaining the applicant's user-row lock: another team's acceptance or team creation can cancel requests while a reviewer waits, and a stale tracked request must not overwrite that cancellation.

Executed SQL concurrency checks proved:

- Two approvals of one request yield one success and one conflict, with one membership.
- Captains of different teams approving one player simultaneously cannot create two memberships.
- Two different applicants competing for the last capacity slot cannot overfill the roster.
- A review waiting behind the user-row gate rejects a request that was concurrently cancelled, preserving the final SQL state.

Captain transfer changes only current team leadership. Integration fixtures verified that frozen `booking_teams.captain_id` and team-name snapshots survive transfer, departure and removal. Tournament/booking snapshots are not rewritten. These fixtures were isolated relationship tests, not implementation of those later modules.

## Security and logos

All Teams pages, APIs and media require the player role; the existing per-request Active-account checks remain. Every management action verifies the current SQL captain/resource relationship. Browser-submitted captain/user/owner/status values cannot assign ownership or membership. Global anti-forgery validation applies to every POST. Inputs have length/range/enum validation, and SQL errors return safe conflicts rather than database details.

Profiles expose names, positions, team metadata and the viewer's own requests; only the captain sees the team's full request queue. Emails, password hashes, ownership documents and other private account fields are not serialized. Search/list queries paginate at twelve teams; request history at twenty rows. Member lists are bounded by the enforced roster capacity.

Optional logos follow the existing field-image validation convention, with a stricter 1 MB cap: JPG/PNG/WebP extension, MIME and actual signature/content checks, generated GUID keys, and private `App_Data/team-logos` storage. SVG, mismatched MIME, fake images, oversized files and invalid storage keys were rejected. Supplied filenames cannot choose paths. An Active former captain was denied logo modification. Image reads use an authenticated media route, never a filesystem path supplied by the browser.

Logo upload is a separate step after the atomic team/captain save. Rejected uploads leave the saved team usable and show a clear warning; edit can retry. Failed logo metadata updates compensate by removing the newly saved file. Only server-owned obsolete logo keys are eligible for cleanup. Legacy external badge URLs remain unchanged in SQL and use initials as the display fallback.

## Database safety, backup and isolation

Because no schema migration was necessary, **no original-database migration, fresh migration backup or migration rehearsal was performed for Phase 4**. The conditional migration procedure was not triggered. No fixture or data migration was executed against original `Kicks`.

The existing separately restored database **`Kicks_BookingTest_20261108_cc8e9a2c`**, already upgraded and verified during Phase 3, was reused. Its starting table contents matched the current original database, including the booking-duration enhancement. All mutating tests used the existing fail-closed C#/PowerShell/JavaScript gates and explicit process-only `KICKS_TEST_CONNECTION`. The normal development connection file remained unchanged.

The existing verified external SQL backup and Git bundles were preserved. The accessible backup copy's SHA-256 was rechecked:

`C:/Users/USER/Desktop/KicksMvc-safety/booking-duration-20261108_cc8e9a2c/Kicks_duration_20261108_cc8e9a2c_verified-copy.bak`

```text
BC3304CC86B70AA1B8CCFD9D19DC6673032C61FD5D1C30118F6059046D44DE5E
```

This retained backup is the previously verified restore source; it predates the Phase 3 duration migration and is **not represented as a new Phase 4 backup**. Existing Phase 3 recovery/migration evidence remains available. A new complete Git bundle, `phase4-start.bundle`, preserves the Phase 4 starting checkpoint in that external safety directory.

## Executed test results

| Suite | Passed | Failed |
|---|---:|---:|
| Teams SQL integration/security/history/concurrency | 91 | 0 |
| Teams Microsoft Edge/Playwright browser flows | 30 | 0 |
| Authentication SQL integration | 84 | 0 |
| Authentication Edge browser regression | 56 | 0 |
| Field management SQL integration | 52 | 0 |
| Field management Edge browser regression | 33 | 0 |
| Booking and duration-enhancement SQL integration | 111 | 0 |
| Booking Edge browser regression | 46 | 0 |
| Fail-closed database/URL/port isolation checks | 15 | 0 |
| **Total** | **518** | **0** |

The authentication route-rendering expectation for Teams was intentionally changed from anonymous access to a login challenge, as required by Phase 4's Active-player access rule. Existing assertions were retained; no security checks were removed. The remaining Phase 1–3 rules, including 60/120-minute bookings, 13:00–23:00 opening hours and the exact three-hour cancellation cutoff, passed again.

Teams browser tests exercised actual Identity login and SQL-backed creation with logo, profile/editing, My team, requests, approval/rejection, leave/removal, transfer, search, refresh persistence and unchanged prototype-team storage. Desktop/tablet/mobile checks at 1440/768/390 pixels passed with no horizontal overflow or JavaScript runtime errors. Screenshot inspection identified a cramped mobile card identity column; a scoped mobile layout adjustment and a new full-width identity assertion passed on rerun.

Release solution/integration-project builds passed with **zero warnings and zero errors**. Model/parity checks and Git whitespace checks passed. Normal-app read-only health checks passed for homepage and SQL Teams asset, with anonymous Teams page/API requests correctly redirected to login.

## Preservation audit

Original database snapshots before/after are identical for **all 35 table-content SHA-256 fingerprints and complete captured schema metadata**, including object IDs, columns/defaults/computed expressions, checks, indexes and all 76 foreign keys. This includes the complete real administrator/player rows, password hashes, approvals/stamps/roles, existing booking/field data, six amenities, team records and the existing migration history. No additional real administrator was created or modified.

The restored test database's before/after table contents and schema also match exactly. Temporary team accounts, rosters, requests, related booking fixtures, fields, logos and all regression-suite fixtures were removed only from the isolated environment; identity counters were not reset. The restored database itself was retained.

All 4,925 files and inventory under the original `C:/Users/USER/Desktop/Kicks` match their saved hashes. Normal development configuration is unchanged. Private application-file inventory is unchanged: zero files at the preserved baseline and completion, with all temporary uploads cleaned. No sensitive configuration, uploaded files, backups or test evidence are included in Git.

## Files changed

Created:

- `ViewModels/TeamViewModels.cs`.
- `Services/Teams/TeamService.cs`, `Services/Teams/TeamLogoStore.cs`.
- `wwwroot/js/server-teams.js`, `wwwroot/css/server-teams.css`.
- `tests/Auth.Integration/TeamTests.cs`, `tests/teams-browser.cjs`.
- This completion report.

Modified:

- `Controllers/TeamsController.cs`: protected SQL routes and media.
- `Program.cs`: two scoped Teams service registrations; existing authentication configuration unchanged.
- `Views/Teams/Index.cshtml`: SQL client/assets and optional field validation using the existing layout/forms.
- `tests/Auth.Integration/Program.cs`: Teams modes and protected-route expectation.
- `README.md`.

No original frontend source, booking/field service, Identity implementation, entity mapping or prior migration was modified. The old shared team prototype implementation remains available to unfinished modules; it is no longer loaded as the Teams page's persistence implementation.

## Evidence, reproduction and limits

Logs, before/after SQL snapshots, preservation JSON, Release/model checks, browser screenshots and test orchestration are under `.local/teams-phase/` (excluded from Git). The final commit/checkpoint information is recorded in `.local/teams-phase/git-verification.json`.

```powershell
$env:KICKS_TEST_CONNECTION='Server=localhost;Database=Kicks_BookingTest_20261108_cc8e9a2c;Trusted_Connection=True;TrustServerCertificate=True;'
dotnet run --project tests/Auth.Integration/Auth.Integration.csproj -c Release -- --teams
node tests/teams-browser.cjs
```

Run mutating fixture suites sequentially. Tests refuse the original database; the normal app continues to use `Kicks`.

The existing one-team-per-player rule is intentional. No team deletion, administrative moderation, multi-team membership, player-profile editing or unrelated module was added. Team-dependent tournaments/challenges/open-games/payment prototypes remain outside this SQL workflow until their own phases; there is no automatic browser-team import or cross-module prototype synchronization. No Phase 5 or payment processing was started.
