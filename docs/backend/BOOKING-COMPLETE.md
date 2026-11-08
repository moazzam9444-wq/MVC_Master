# KICKS Backend Phase 3 — Booking and availability completion report

**Implemented and verified: 325 automated assertions passed, 0 failed in final runs.** Release build: zero warnings/errors. EF reports no model changes; no migration or schema alteration was required. Phase 4 was not started.

## Features and approved rules

- SQL availability, signed server-price quotes, reservation creation, scoped player/owner lists, detail dialogs, search, pagination, upcoming/history filters, cancellation and owner completion.
- Existing SQL `ends_at` is computed as `DATEADD(HOUR,1,starts_at)`. This phase therefore supports exactly **60 minutes**, with full-hour starts **10:00 through 22:00**, ending no later than **23:00**. Starts retain the existing SQL **UTC+03:00** constraint and display as Amman time. The existing UI exposed these starts on each date and the schema has no per-field weekday/opening schedule; these inherited global hours are retained, rather than inventing 24-hour availability.
- No past start can be quoted or newly reserved. Public availability supplies thirteen server-calculated slots for one requested date. Archived fields, inactive owners and accounts without owner membership are ineligible.
- The project owner explicitly selected **Confirmed reservations with all payment settlement deferred**. No owner-approval or payment hold was added. No payment, team or split-payment rows are created.
- The project owner explicitly required players to cancel only their own **Confirmed** reservations with **at least three hours remaining**. Owners may cancel **future** Pending/Confirmed bookings for their own fields without the three-hour restriction. Only owners can mark **ended Confirmed** sessions Completed.

## Database integration and pricing

The module reuses `bookings`, `fields`, `users`, the existing foreign keys, the filtered active-booking unique index, the four booking status values and cancellation audit columns. Dedicated input/query/display models avoid entity binding. Player ID comes from authenticated Identity, and owner ID comes from the selected SQL field. Forged price, user, owner, payment and status fields cannot set booking values.

The server reads the field's `hourly_price` and stores the agreed one-hour total in the existing `bookings.price`. This preserves price history after later field-price edits. A protected quote binds the current player, field, start, price, history watermark and fifteen-minute expiry. Confirmation locks the field and rechecks eligibility, start time, price and occupancy. A changed price or expired unused quote returns HTTP 409 and requires a newly displayed quote; the server never silently accepts a higher price.

CommissionRate and Commission are zero because settlement is deferred. OwnerNet equals the reservation price solely to satisfy the existing arithmetic constraint `commission + owner_net = price`; it is an **unsettled allocation, not paid earnings or a payout**. No payment record is fabricated or marked paid, and cancellation never edits payment records or issues a refund.

## Concurrency and overlap prevention

All booking creates, cancellations and completion transitions acquire a SQL Server **UPDLOCK, HOLDLOCK on the field row inside a transaction**. Phase 2 edits/archive already use that same field-row lock, so price changes and archival serialize with creation. Occupancy is checked again under this lock before insertion. This works across application instances; no browser availability flag or process-local mutex is authoritative.

Intervals are half-open: a row conflicts when `existing.StartsAt < requestedEnd && existing.EndsAt > requestedStart`. Adjacent sessions are allowed without a buffer. **Pending and Confirmed bookings block**; Cancelled and Completed bookings do not. The existing filtered unique `(field_id, starts_at)` index is retained as an additional database guard.

Existing non-cancelled SQL open-game/tournament intervals are also read as occupancy, matching the earlier shared-field prototype rule. No CRUD or other business workflow for those modules was implemented. Their future SQL writers must use the same field-lock protocol to preserve cross-module concurrency safety.

The real concurrent integration test releases two separately authenticated players' HTTP confirmation requests together, with separate request/database contexts and valid quotes for the same slot. Exactly one returned **201 Created**, one returned **409 Conflict**, and SQL contained exactly one blocking reservation.

## Duplicate confirmation handling

A protected quote includes the latest prior booking ID for that player/field/start. Under the field lock, confirmation first searches for the **first matching persisted booking after that signed history watermark**. A retry returns that record with HTTP 200 instead of inserting again. This durable replay works after refresh/restart and does not require a new database column or an in-memory cache.

The persisted result is returned even if it has since been cancelled or completed. An old confirmation cannot recreate a cancelled reservation, and its response/confirmation UI reports the actual returned status. A genuinely new quote after cancellation has a newer watermark and can create a new reservation if the slot remains available. Quotes issued while a slot is occupied are refused. Rapid confirmation clicks are disabled in the UI; both SQL retry and browser rapid-click tests passed.

## Authorization, cancellation and lifecycle

Management routes use standard Identity role authorization and the existing per-request Active/status/security-stamp checks. Player queries filter by authenticated UserId. Owner queries require both the booking owner snapshot and current field ownership to match the authenticated owner. Foreign IDs return 404, while unauthorized roles challenge/deny. Mutating MVC requests retain global anti-forgery validation.

| Actor / current state | Allowed transition | Server condition |
|---|---|---|
| Player / Confirmed | Cancelled | Own booking; start minus server UTC now is at least three hours |
| Player / Pending | None | Contact the field owner |
| Owner / Pending or Confirmed | Cancelled | Own field; start is strictly in the future |
| Owner / Confirmed | Completed | Own field; end is at or before server UTC now |
| Any / Cancelled or Completed | None | Immutable historical outcome |

The cutoff uses SQL DateTimeOffset starts and `TimeProvider.GetUtcNow()`, not browser time. Tests accepted cancellation at exactly three hours and rejected it one second inside the cutoff with a clear message. Owner cancellation within that window also passed. Cancellation writes reason Cancelled, actor ID and timestamp, retaining the row and releasing its slot.

Status forms carry a protected fingerprint of booking identity, start/end, agreed price, status and audit state. A stale/tampered version returns 409 after the resource is re-read under the field lock. Split/challenge/team-linked bookings are visible only within the ordinary resource scope and are not mutated by this phase's simple-reservation workflow. No refund/payment mutation occurs.

Historical Confirmed sessions appear under History with “Session ended” until the owner explicitly completes them. GET requests do not silently rewrite historical statuses. No production physical-delete endpoint was added.

## Frontend integration

Field Details now enables the existing date/slot/summary/confirmation controls using SQL availability. The one supported duration is shown explicitly. Confirmation shows the server quote and clearly states that no payment is collected. The old Visa/demo copy was removed; existing dialog IDs/classes are retained for compatible styling, not for demo behavior. A lost slot/price conflict closes confirmation, explains the problem and refreshes live slots.

Player and Owner Dashboard → Bookings now load SQL Razor panels with field/location, date/time, duration, price, status, details and permitted actions. Search, period filters and pagination use bounded server queries (twelve results per page). Relevant overview booking data also comes from a bounded SQL snapshot, replacing browser booking state in those migrated dashboard flows. Detailed lists show all scoped records through pagination. Admin-wide booking management was not added; it belongs to a separately defined administrative scope.

The SQL booking client uses shared escaping/navigation utilities but never calls prototype booking or payment persistence. A browser assertion verified prototype booking storage remained unchanged after a real SQL reservation. Shared stores remain available for unfinished game, team, tournament and challenge prototypes.

Desktop/tablet/mobile KICKS styling was retained. A scoped panel layout avoids an old composition adapter that made filter labels low-contrast. Booking filters, tables, confirmation and availability were inspected, with no horizontal overflow at **1440, 768 and 390 pixels**.

## Executed tests and regressions

| Final suite | Passed | Failed |
|---|---:|---:|
| SQL booking integration and concurrency | 63 | 0 |
| Actual MVC booking browser workflows, Microsoft Edge | 37 | 0 |
| Existing authentication SQL integration | 84 | 0 |
| Existing authentication browser regression | 56 | 0 |
| Existing field SQL integration | 52 | 0 |
| Existing field browser regression | 33 | 0 |
| **Total** | **325** | **0** |

SQL tests exercised valid creation, fresh-context persistence, player/owner/field associations, server price and financial-deferment fields, forged input, past dates, duration/hours, missing fields, archived/inactive-owner rejection at quote and confirmation, Pending/Confirmed occupancy, adjacency, existing activity intervals, simultaneous conflicting requests, duplicate replay, price changes, expiry, IDOR, anonymous denial, CSRF on create/cancel, exact cancellation cutoff, owner late cancellation, stale status commands, cancelled history, completion restrictions, bounded pagination and existing field access.

Browser tests exercised actual Identity login, catalogue navigation, SQL details/availability, server-priced confirmation, rapid clicks, no prototype booking writes, persisted confirmation reference, My Bookings, details, refresh and fresh contexts, two valid competing quotes followed by a real HTTP 409 and refreshed slots, adjacent reservations, owner visibility/IDOR, player/owner cancellations, owner completion of an ended fixture, history filtering, invalid date input and responsive layouts. No booking-flow JavaScript runtime errors occurred.

All 225 previous-phase assertions passed again. The sole intentional Phase 2 expectation change replaced “booking remains disabled” with verification of the newly operational SQL availability interface. Authentication review tests continued using the isolated test-host principal; no real administrator password was requested or used.

Additional checks passed: full Release solution build with zero warnings/errors; offline EF model/SQL parity; no-pending-model-change check; live schema/catalog comparison; cryptographic SQL data preservation; private/public file checks; original frontend hashes/inventory.

## Bugs discovered and fixed

- A test harness variable-name collision was corrected before its first successful SQL run.
- Visual inspection found the older dashboard composition adapter applying inappropriate contrast/layout to the new filters. The SQL panel now opts out of that transformation and uses scoped KICKS styles; the affected browser suite was rerun.
- The remaining demo-checkout label was replaced with reservation terminology.
- Code review caught a replay-response edge case: returning a cancelled reservation must not say “Confirmed.” The controller and confirmation UI now use the actual persisted status. SQL replay-after-cancellation and browser confirmations passed after the fix.
- No failing assertion remains in the final runs.

## Preservation and evidence

All **35 physical tables** match their pre-test contents by SHA-256 of ordered JSON computed inside SQL Server. The complete real administrator/player rows, passwords/hashes, approval, stamps, roles, six reference amenities, migration history and unrelated business data are unchanged. Raw credentials/password hashes were not exported into evidence.

The live schema, table/constraint object IDs and **76 foreign keys** are unchanged. There is no migration or model change. No baseline was reapplied and no identity counter was reset; normal gaps from removed isolated fixtures remain.

Only uniquely marked temporary accounts, fields, bookings and the isolated occupancy fixture were removed. Test cleanup additionally restricts booking/player and activity/organizer IDs to the temporary users; foreign relationships would prevent cleanup rather than be cascade-deleted. Private ownership files and venue images match the baseline, with no test field images remaining. All **4,925 original frontend files** under `C:/Users/USER/Desktop/Kicks` retain their original hashes and inventory.

Relevant evidence is in `.local/booking-phase/`: `integration.log`, `browser.log`, authentication/field regression logs, `model-check.json`, schema/data before/after snapshots, `preservation.json`, booking screenshots and copied regression screenshot folders. Test listeners were stopped; the user's original application process on port 5602 was preserved.

## Files created and modified

Created:

- `ViewModels/BookingViewModels.cs`.
- `Services/Bookings/BookingService.cs`.
- `Controllers/BookingsController.cs`.
- `Views/Shared/_Bookings.cshtml`.
- `wwwroot/js/server-bookings.js`, `wwwroot/css/server-bookings.css`.
- `tests/Auth.Integration/BookingTests.cs`, `tests/booking-browser.cjs`.
- This report; a copy is saved under `.local/booking-phase/completion-report.md`.

Modified:

- `Program.cs`: BookingService and TimeProvider registration; prior Identity and field settings preserved.
- `Views/Fields/Details.cshtml`, `wwwroot/js/field-details.js`: live reservation integration and confirmation copy.
- `Views/Dashboard/Player.cshtml`, `Views/Dashboard/Owner.cshtml`: booking assets and refreshed asset versions.
- `wwwroot/js/dashboard.js`, `dashboard-overview.js`: scoped SQL booking panels and overview data.
- `tests/Auth.Integration/Program.cs`: explicit booking suite/fixture modes, excluded from the MVC application.
- `tests/field-browser.cjs`: replace the obsolete disabled-booking assertion.
- `README.md`.

No entity, DbContext, migration, authentication-service implementation or original frontend source was changed.

## Manual testing and reproduction

1. Restart the MVC app with `dotnet run --project KicksMvc.csproj --launch-profile KicksMvc` to load the rebuilt Razor/assets.
2. Ensure an approved owner has a published field. Sign in as an approved player, open its catalogue card, choose a future date and one of the available hourly sessions.
3. Review the quoted JOD price and confirm. Check the reservation reference and My Bookings; refresh or use a fresh session to verify persistence.
4. In a second player browser, try a stale quote for the same slot. Exactly one active reservation may succeed; the other must show a conflict and refresh slots.
5. Cancel your own Confirmed booking more than three hours ahead. Its history must remain and availability return. Within three hours, the server rejects player cancellation and directs you to the field owner.
6. Sign in as the field owner. Bookings for that owner's fields are visible; future reservations can be cancelled without the three-hour cutoff, and ended Confirmed sessions can be completed. Neither action changes payment records.
7. Try a foreign booking ID or another owner's route, missing tokens, past dates, stale versions or a price change between quote and confirmation. The server must deny or conflict safely.

```powershell
dotnet build KicksMvc.sln --configuration Release
dotnet run --project tests/Auth.Integration/Auth.Integration.csproj --configuration Release -- --bookings
node tests/booking-browser.cjs
dotnet run --project tests/Auth.Integration/Auth.Integration.csproj --configuration Release
dotnet run --project tests/Auth.Integration/Auth.Integration.csproj --configuration Release -- --fields
node tests/field-browser.cjs
node tests/ef-model-preparation-check.cjs
dotnet ef migrations has-pending-model-changes --context KicksDbContext --configuration Release --no-build
```

The booking browser suite starts/stops the built app on loopback port 5616 and cleans up its fixtures. The field browser suite uses 5615. Playwright and Microsoft Edge are required. To repeat the authentication browser suite, run the app on an isolated loopback port, set `KICKS_TEST_URL` to it, and run `node tests/auth-final-browser.cjs`; its private review bridge uses 5613. Run mutating fixture suites sequentially when comparing whole-database snapshots.

## Remaining limitations and recommended Phase 4

One-hour sessions and the inherited global opening hours are intentional current-schema limits. There are no per-field operating calendars, holiday overrides, rescheduling, arbitrary duration or owner-transfer workflows. Catalogue/homepage broad “available now/today” filters remain deferred; the field detail date/slot availability is operational. Overview snapshots are bounded highlights, with full scoped history in the paginated booking panel.

Quotes do not hold slots. Availability is advisory until the protected confirmation transaction succeeds. History-based replay relies on retained bookings and monotonically increasing IDs; production history is never deleted and identity counters must not be reset. Ended sessions are not automatically marked Completed. General split/team/challenge booking mutations and administrative global booking management remain outside this module.

Recommended Phase 4: an explicitly reviewed payment/settlement lifecycle, with durable payment idempotency, provider/webhook verification and refund rules. A Confirmed reservation must never be interpreted as paid. No payment gateway, refunds, team/split-payment business logic or other later module was implemented here.
