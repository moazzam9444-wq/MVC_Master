# KICKS MVC — EF Core preparation report

Date: 7 November 2026.

Project: `C:/Users/USER/Desktop/KicksMvc`.

**Outcome:** The approved EF corrections are implemented and pass offline model validation. The first migration must wait for the Identity architecture decision. No database was created, connected to, or modified. No SQL or migration command was executed.

## 1. Scope and files

Changed existing files:

- [KicksDbContext](C:/Users/USER/Desktop/KicksMvc/Data/KicksDbContext.cs): key semantics, typed defaults, navigations/cardinality, and index reconciliation.
- 26 existing entity files in [Models/Entities](C:/Users/USER/Desktop/KicksMvc/Models/Entities): SQL-defined literal defaults and/or navigation properties. The complete per-entity inventory is below. `Feedback.cs` is unchanged.
- [Program](C:/Users/USER/Desktop/KicksMvc/Program.cs): extended the existing offline model-inspection branch with detailed metadata and optional in-memory behavior checks. Ordinary MVC startup, routes, and service registration are unchanged.

New maintained files:

- [KicksModelInspection](C:/Users/USER/Desktop/KicksMvc/Data/Diagnostics/KicksModelInspection.cs): offline EF metadata inspection and change-tracker checks. No connection opening, data saving, SQL generation, or migration calls.
- [ef-model-preparation-check.cjs](C:/Users/USER/Desktop/KicksMvc/tests/ef-model-preparation-check.cjs): compares the actual finalized SQL Server EF model against the original SQL text without executing that text.
- This report.

Working evidence is under `.local/ef-preparation/` in KicksMvc: before/after metadata, original/protected-file hashes, validation output, and the MVC route smoke-check output. This folder is already ignored. No project/package reference was added.

Unchanged: all Views, layouts, JavaScript, CSS, assets, controllers, ViewModels, application configuration, project/solution files, SQL, and existing schema-documentation sources. The original `C:/Users/USER/Desktop/Kicks` project is unchanged.

## 2. Alternate keys converted to unique indexes

All existing principal-key dependencies were inspected before conversion. The following 12 uniqueness-only alternate keys became unfiltered unique indexes. Their existing `UQ_...` database names were retained for traceability to the original SQL design.

| Table | Unique columns |
|---|---|
| `users` | `email` |
| `field_images` | `field_id, sort_order` |
| `amenities` | `name` |
| `teams` | `captain_id` |
| `teams` | `name` |
| `tournament_registrations` | `tournament_id, team_id` |
| `tournament_registrations` | `tournament_id, captain_id` |
| `tournament_draw_entries` | `tournament_id, seed_position` |
| `tournament_matches` | `tournament_id, round_number, match_number` |
| `game_participants` | `game_id, user_id` |
| `booking_shares` | `booking_id, team_id` |
| `booking_shares` | `booking_id, payer_id` |

This preserves uniqueness while allowing EF to track ordinary edits. It does not authorize business operations such as changing a paid registration or frozen booking. Those restrictions still belong in the future application services.

The offline checks confirm tracked edits for representative fields from all 12 indexes, including email, captaincy, seed ordering, and share foreign keys. SQL conflict handling and safe multi-row reordering still need database integration tests.

Reference: [Microsoft EF key guidance](https://learn.microsoft.com/en-us/ef/core/modeling/keys#alternate-keys) explains that alternate keys carry identity semantics and uniqueness-only values should use unique indexes.

## 3. Alternate keys intentionally preserved

| Table | Alternate-key columns | Why it remains |
|---|---|---|
| `tournament_registrations` | `tournament_id, id` | Composite FKs keep draw entries, winners, and match slots in the correct tournament. |
| `tournament_matches` | `tournament_id, id` | Composite FKs keep match inputs and feeds in the correct tournament. |
| `booking_teams` | `booking_id, team_id, captain_id` | Composite FK binds a share to its agreed, frozen captain and team. |

All 27 primary keys are unchanged. After correction there are **3 alternate keys plus 12 unique indexes replacing the other UNIQUE constraints**. These preserve all 15 original uniqueness guarantees; the migration will intentionally use different physical object kinds for those 12 rules. The original SQL file still contains its original 15 UNIQUE constraints and was not edited.

## 4. Default-value corrections

All **39 SQL literal defaults** now have matching CLR initializers and typed `HasDefaultValue(...)` configuration. Examples include:

- User: `player`, `Pending`, the existing password-algorithm marker and iteration count.
- Field/challenge/team format: `5v5`.
- Team: `Intermediate`, capacity 12, `forest`, recruiting enabled.
- Request statuses: `Pending`; team request type `join`.
- Positions: `Midfielder` where SQL already defines that default.
- Tournament/open-game status: `Scheduled`; tournament system: `Single elimination`.
- Booking status: `Confirmed`, preserving the existing SQL rule rather than inventing a new pending-booking default.
- Share state: `Unpaid`; receipt status: `Paid (demo)`; demo flag enabled.
- Settings: singleton ID 1, commission 10, listing fee 5.

For literal-default properties, `ValueGeneratedNever()` makes EF send the explicitly initialized/current CLR value. The SQL default constraint remains represented for non-EF inserts. This also preserves intentionally supplied `0` and `false`, particularly zero listing fees/commission and disabled recruitment/demo flags. It avoids treating valid zero/false values as an instruction to substitute a nonzero/true database default.

All **16 server timestamp defaults** remain `SYSUTCDATETIME()` and generated on insertion. All **3 persisted computed end-time columns** remain generated on add/update. Required input fields without SQL defaults still require callers to supply their real values; this change does not fabricate names, credentials, prices, schedules, or users.

Reference: [Microsoft generated-value guidance](https://learn.microsoft.com/en-us/ef/core/modeling/generated-properties).

## 5. Navigations and cardinality

Added **73 navigation properties across 44 existing FK relationships**, using the existing FK columns and constraint names. No table, mapped scalar property, shadow FK, or duplicate relationship was introduced.

Examples:

- `Field.Owner` / `User.OwnedFields`, images and amenities.
- `Team.Captain` / `User.CaptainedTeam` as one-to-one; one player can captain at most one team.
- `TeamMember.User` / `User.TeamMembership` as one-to-one; the existing user-keyed membership prevents two permanent teams.
- `Team.Members`, `RecruitingPositions`, and `Requests`.
- `Tournament.Registrations`, `DrawEntries`, `Matches`; registration players and its optional draw entry.
- `TournamentMatche.InputSlots` / `OutputSlots`, using the same-tournament composite FK relationships for match/feeder navigations.
- `Booking.Teams`, `Shares`, and optional challenge association.
- `BookingShare.BookingTeam` / `BookingTeam.Share` as one-to-one using the frozen-captain composite key.
- `BookingShare.Payment` / `Payment.Share` as one-to-one using the existing unique share receipt rule.
- Message sender/recipient references.

Where SQL has both a simple FK and a composite FK protecting the same records, both constraints remain. A navigation is attached to one appropriate relationship; the other remains navigation-free. This is intentional and avoids duplicating relationships.

All delete behaviors remain `NoAction`. Navigation cycles are normal for this model; future endpoints should return dedicated DTOs rather than serialize complete tracked entity graphs. The existing `TournamentMatche` spelling is retained to avoid an unrelated rename.

## 6. Index reconciliation

Original SQL: **60 explicit indexes**. Original finalized EF model: **54 indexes**, including convention indexes; only 9 had explicit source configurations.

Corrected model:

- **48** original SQL indexes explicitly mapped with their original names, columns, uniqueness, and filters.
- **12** original single-prefix indexes covered by existing keys or longer unfiltered indexes, instead of separate duplicate indexes.
- **12** new explicit unique-index mappings replacing uniqueness-only alternate keys.
- **6** supplementary EF convention indexes supporting composite FK/cardinality metadata.
- **66 total model indexes**, excluding the indexes backing primary and alternate keys.

The six supplementary indexes are on:

1. `booking_shares(booking_id, team_id, payer_id)` — unique, from the composite one-to-one share relationship.
2. `tournament_draw_entries(tournament_id, registration_id)`.
3. `tournament_matches(tournament_id, winner_registration_id)`.
4. `tournament_match_slots(tournament_id, match_id)`.
5. `tournament_match_slots(tournament_id, registration_id)`.
6. `tournament_match_slots(tournament_id, source_match_id)`.

The first is logically redundant with the shorter unique booking/team index, but is convention-generated for the explicit composite one-to-one relationship. It is retained rather than introducing custom convention manipulation in this preparation pass. The initial migration should make this expected overlap visible for review; query-performance benefits are not claimed without SQL testing.

All six original filtered unique indexes are unchanged in meaning:

- Pending team requests: team/user/request type.
- Pending challenge requests: challenge/team.
- Accepted challenge rival: challenge.
- Active booking: field/start time for Pending or Confirmed rows.
- Linked challenge booking: non-null challenge ID.
- Share receipt: non-null share ID.

No existing database index was dropped: no database was accessed. The omitted duplicate *mappings* and their covering keys/indexes are listed in the generated reconciliation appendix below. Covered access paths do not imply identical execution plans; performance remains a database-test concern.

## 7. Integrity and validation results

Commands executed from KicksMvc:

```powershell
dotnet build KicksMvc.sln --no-restore --configuration Release --nologo
node tests/ef-model-preparation-check.cjs
```

Results:

| Verification | Result |
|---|---|
| Solution Release build | PASS; 0 warnings, 0 errors |
| Tables / entity classes / DbSets | 27 / 27 / 27 |
| Scalar columns | 230, unchanged |
| Ordinary column types/nullability/identity | All 227 match original SQL |
| Persisted computed columns | All 3 expressions and generation behavior preserved |
| Primary keys | All 27 names and column orders preserved |
| Foreign keys | All 68 names, tables, column orders, requiredness, and NoAction deletes preserved |
| CHECK constraints | All 77 names and expressions match original SQL |
| Original UNIQUE guarantees | All 15 preserved as 3 alternate keys and 12 unique indexes |
| Original index access paths/filtered uniqueness | All 60 accounted for |
| Literal/default timestamp mappings | 39 / 16 verified |
| Decimal and DateTimeOffset SQL types | Preserved |
| Booking timezone/hour CHECK | Preserved exactly |
| Navigation properties / shadow properties | 73 / 0 |
| Offline behavior checks | 22 passed |
| Razor routes | 20 returned HTTP 200 with HTML |
| Static JS smoke checks | 4 returned HTTP 200 |

Behavior checks cover the 12 former alternate-key edit paths, important inverse navigation fixups, frozen captain matching, tournament composite match feeds, and explicit zero/false generation behavior. They use disposable in-memory change-tracker objects only, with no stored or seeded accounts.

The route smoke test starts a temporary MVC process with database context registration explicitly disabled, fetches all routes from the existing route manifest, then stops that process. It does not run browser workflows or alter browser storage. Hash comparisons verify all protected MVC files and original-project files against the pre-edit baseline; details are recorded in the preservation appendix below.

**Limits:** No SQL Server connection or runtime DDL/DML test was performed. Database default execution, SQL Server metadata, check/index enforcement, execution plans, payment concurrency, and migration application are not certified by these offline tests. No migration or model snapshot was generated. No package was installed/restored. No authentication, registration, login, business service, or payment implementation was added.

## 8. Identity architecture — design only, approval required

### Recommendation

For this project, prefer the standard EF Identity stores with an application user based on **`IdentityUser<int>`**, mapped to the existing `dbo.users` table. Preserve its integer primary key so all existing domain relationships can continue referencing the same user IDs. Keep profile fields and Pending/Active/Rejected/Inactive review state as application-user data.

This offers the simplest maintenance path for one MVC application with many existing user FKs. It does require an approved schema change; the current `User` remains unchanged in inheritance in this task.

### Alternatives considered

| Option | Assessment |
|---|---|
| Extend `IdentityUser<int>` and map to `users` | Recommended: standard managers/stores, one account identity, existing integer relationships retained. Requires Identity columns/tables and a controlled credential/role transition. |
| Custom Identity store over the current schema | Useful if an immutable external schema is mandatory. Otherwise adds substantial implementation/testing for password, email, roles, lockout, tokens, stamps and concurrency. Not recommended solely to keep the current column list. |
| Separate Identity user linked one-to-one to domain `User` | Valid when authentication is a separate bounded context or domain users must remain independent. Adds link integrity, account/profile lifecycle coordination, and possible duplicated email/status data. More complexity than this application currently needs. |

### Schema implications of the recommendation

- Add/map Identity username and normalized username/email, confirmation flags, security/concurrency stamps, phone confirmation, two-factor, lockout, and failed-access fields with reviewed nullability and lengths.
- Add the selected Identity role, user-role, claim, login, and token tables. Exact additions depend on the selected Identity schema/version/features, including whether passkeys are enabled. The final integrated schema will exceed the current 27 domain tables.
- Use Identity roles as the authorization authority while preserving the player/owner/admin concepts. The current single-role rule needs an explicit policy/enforcement decision because standard Identity supports multiple role assignments.
- Decide whether the old `users.role` remains a read-only compatibility field or is retired in an approved schema change. Do not maintain two independent authorities for permissions.
- The custom password salt/algorithm/iteration fields and their CHECK constraint require an explicit retirement or legacy-transition decision. They must not be assumed to describe Identity's password-hash format.
- Do not copy browser password hashes or the demo bootstrap administrator into production authentication. Existing real accounts, if any are later imported, need a reviewed reset or compatible rehash transition. Administrator provisioning must be controlled separately.
- Email login normalization/uniqueness and approval/deactivation behavior must be enforced consistently on the server. A valid password must not bypass the application's account-approval state.

**Blocker before the initial integrated migration:** approve the Identity user/table strategy and credential/role transition. No Identity packages, inheritance, stores, tables, roles, users, or migrations were implemented here.

Sources: [Identity model customization](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/customize-identity-model?view=aspnetcore-10.0), [custom storage providers](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-custom-storage-providers?view=aspnetcore-10.0).

## 9. Concurrency and business-rule decisions — not implemented

| Workflow | Existing database protection | Required future decision/implementation |
|---|---|---|
| Venue allocation | Unique active booking field/hour | All bookings, games, and tournaments must share a transaction/locking protocol for the field. A shared occupancy table is optional, not automatically required. |
| Tournament capacity | Unique team/captain entries | Lock the tournament aggregate while checking active entries and registering; coordinate with draw start. |
| Open-game capacity | Unique game/player entry | Lock the game aggregate while counting and inserting/reactivating participants. |
| Team membership | User-keyed membership, unique captain | Lock capacity/membership decisions; atomically add/remove membership and resolve requests. |
| Challenge acceptance | One accepted request per challenge | Lock challenge/team/venue decisions in a consistent order; create frozen booking and two shares atomically. |
| Split payments | Unique receipt per share, balanced amounts, frozen captain FK | Lock the booking aggregate; enforce exact two shares, totals, payer/amount matching, confirmation, expiry and refunds atomically. |
| Payment retries | Share uniqueness only | Approve persisted idempotency/request-attempt design before retriable payment endpoints. Preserve legitimate history when entries are reused. |
| Bracket changes | Same-tournament FKs and numbered slots | Lock/version the tournament result command, validate pairings/acyclic feeds, clear dependent winners and serialize draw-start/registration. |

### Potential schema changes needing approval

1. **Concurrency tokens:** recommended for mutable workflow aggregates/results. Select the tables and `rowversion` or equivalent policy before freezing the model. Tokens do not alone protect capacity or interval overlap; commands must touch/check the coordinating aggregate or use locking.
2. **Payment idempotency:** decide a unique operation key versus an explicit attempt/intent structure. Important before backend retry handling; real-provider identifiers/webhook deduplication are additional future requirements.
3. **Slot representation checks:** decide whether direct entry and source match are mutually exclusive, and how empty/byes are represented, before adding stronger row-level checks.
4. **State/timestamp consistency:** approve transition rules before strengthening review, withdrawal, cancellation, payment and refund checks.
5. **Group-stage support:** the UI and SQL allow a group-stage label, but current bracket code is knockout-only. Supporting actual groups needs an approved groups/results/standings design; limiting the feature also needs an explicit product decision. Existing values/rules were not removed.

Only the Identity design is an unconditional architectural blocker for the proposed Identity-integrated initial migration. Other schema additions depend on the approved concurrency/payment/product strategy and may be handled by deliberate later migrations; their required application guarantees cannot be skipped. None was added speculatively.

## 10. Migration compatibility and next action

- The SQL script remains the original baseline artifact. Its 12 converted physical UNIQUE constraints intentionally differ from the corrected unique-index model, while retaining their uniqueness guarantees.
- EF now generates literal SQL defaults from typed values. SQL text/default-constraint names can differ while the reviewed values remain equivalent.
- Supplementary EF composite/cardinality indexes and omitted covered indexes are expected differences, not dropped relationships.
- Existing stored data would still need validation for duplicate values and compatibility before applying any eventual migration. No live database was inspected.
- Mutable unique values can still conflict at SQL level; batch reordering needs a separately designed transaction strategy. Removing EF alternate-key immutability does not solve temporary duplicate positions.
- Keep one owned schema evolution process after approval, and deliberately reconcile exported SQL and schema documentation with reviewed EF migrations.

**Next action:** review/approve the Identity recommendation and the initial concurrency/payment scope. Then prepare and inspect the first migration in a separate authorized task. No database or migration action follows automatically from this report.

## Appendix A. Complete per-entity change inventory

<!-- Generated from finalized EF metadata and the pre-edit hash baseline. -->

| Entity file | Status | SQL-literal default properties initialized | Navigation properties added |
|---|---|---|---|
| [Amenity](C:/Users/USER/Desktop/KicksMvc/Models/Entities/Amenity.cs:3) | Modified |  | `FieldAmenities` |
| [BookingShare](C:/Users/USER/Desktop/KicksMvc/Models/Entities/BookingShare.cs:3) | Modified | `PaymentState` | `Booking`, `BookingTeam`, `Payer`, `Payment` |
| [BookingTeam](C:/Users/USER/Desktop/KicksMvc/Models/Entities/BookingTeam.cs:3) | Modified |  | `Booking`, `Captain`, `Share`, `Team` |
| [Booking](C:/Users/USER/Desktop/KicksMvc/Models/Entities/Booking.cs:3) | Modified | `IsSplit`, `Status` | `Challenge`, `Field`, `Owner`, `Player`, `Shares`, `Teams` |
| [ChallengeRequest](C:/Users/USER/Desktop/KicksMvc/Models/Entities/ChallengeRequest.cs:3) | Modified | `Status` | `Challenge`, `Team` |
| [Challenge](C:/Users/USER/Desktop/KicksMvc/Models/Entities/Challenge.cs:3) | Modified | `Format`, `PaymentMode`, `CostAgreement`, `Status` | `Booking`, `Field`, `HostTeam`, `OpponentTeam`, `Requests` |
| [Feedback](C:/Users/USER/Desktop/KicksMvc/Models/Entities/Feedback.cs:3) | Unchanged |  |  |
| [FieldAmenity](C:/Users/USER/Desktop/KicksMvc/Models/Entities/FieldAmenity.cs:3) | Modified |  | `Amenity`, `Field` |
| [FieldImage](C:/Users/USER/Desktop/KicksMvc/Models/Entities/FieldImage.cs:3) | Modified | `SortOrder` | `Field` |
| [Field](C:/Users/USER/Desktop/KicksMvc/Models/Entities/Field.cs:3) | Modified | `Format`, `Rating`, `LocationPrecision`, `IsArchived` | `Bookings`, `FieldAmenities`, `Images`, `OpenGames`, `Owner`, `Tournaments` |
| [GameParticipant](C:/Users/USER/Desktop/KicksMvc/Models/Entities/GameParticipant.cs:3) | Modified | `Position`, `Status` | `Game`, `User` |
| [Message](C:/Users/USER/Desktop/KicksMvc/Models/Entities/Message.cs:3) | Modified |  | `Recipient`, `Sender` |
| [OpenGame](C:/Users/USER/Desktop/KicksMvc/Models/Entities/OpenGame.cs:3) | Modified | `Status` | `Field`, `Participants` |
| [OwnershipDocument](C:/Users/USER/Desktop/KicksMvc/Models/Entities/OwnershipDocument.cs:3) | Modified |  | `User` |
| [Payment](C:/Users/USER/Desktop/KicksMvc/Models/Entities/Payment.cs:3) | Modified | `Status`, `IsDemo` | `Booking`, `GameParticipant`, `Registration`, `Share` |
| [RegistrationPlayer](C:/Users/USER/Desktop/KicksMvc/Models/Entities/RegistrationPlayer.cs:3) | Modified | `Position` | `Registration` |
| [Setting](C:/Users/USER/Desktop/KicksMvc/Models/Entities/Setting.cs:3) | Modified | `Id`, `CommissionRate`, `ListingFee` |  |
| [TeamMember](C:/Users/USER/Desktop/KicksMvc/Models/Entities/TeamMember.cs:3) | Modified | `Position` | `Team`, `User` |
| [TeamRecruitingPosition](C:/Users/USER/Desktop/KicksMvc/Models/Entities/TeamRecruitingPosition.cs:3) | Modified |  | `Team` |
| [TeamRequest](C:/Users/USER/Desktop/KicksMvc/Models/Entities/TeamRequest.cs:3) | Modified | `RequestType`, `Status`, `Position` | `Team` |
| [Team](C:/Users/USER/Desktop/KicksMvc/Models/Entities/Team.cs:3) | Modified | `Level`, `Format`, `Capacity`, `Color`, `IsRecruiting` | `Captain`, `Members`, `RecruitingPositions`, `Requests`, `TournamentRegistrations` |
| [TournamentDrawEntry](C:/Users/USER/Desktop/KicksMvc/Models/Entities/TournamentDrawEntry.cs:3) | Modified |  | `Registration`, `Tournament` |
| [TournamentMatchSlot](C:/Users/USER/Desktop/KicksMvc/Models/Entities/TournamentMatchSlot.cs:3) | Modified |  | `Match`, `Registration`, `SourceMatch` |
| [TournamentMatche](C:/Users/USER/Desktop/KicksMvc/Models/Entities/TournamentMatche.cs:3) | Modified | `IsBye` | `InputSlots`, `OutputSlots`, `Tournament` |
| [TournamentRegistration](C:/Users/USER/Desktop/KicksMvc/Models/Entities/TournamentRegistration.cs:3) | Modified | `Status` | `DrawEntry`, `MatchSlots`, `Players`, `Team`, `Tournament` |
| [Tournament](C:/Users/USER/Desktop/KicksMvc/Models/Entities/Tournament.cs:3) | Modified | `System`, `Status` | `DrawEntries`, `Field`, `Matches`, `Registrations` |
| [User](C:/Users/USER/Desktop/KicksMvc/Models/Entities/User.cs:3) | Modified | `PasswordAlgorithm`, `PasswordIterations`, `Role`, `Status` | `CaptainedTeam`, `OwnedFields`, `OwnershipDocuments`, `TeamMembership` |

## Appendix B. Original index mappings covered without a separate index

These are covered in the finalized EF model; the original SQL text remains unchanged.

| Table | Original SQL index | Leading columns | Covering key/index |
|---|---|---|---|
| `field_images` | `IX_field_images_0` | `field_id` | `UQ_field_images_field_id_sort_order` |
| `field_amenities` | `IX_field_amenities_0` | `field_id` | `PK_field_amenities` |
| `team_recruiting_positions` | `IX_team_recruiting_positions_0` | `team_id` | `PK_team_recruiting_positions` |
| `tournament_registrations` | `IX_tournament_registrations_0` | `tournament_id` | `UQ_tournament_registrations_tournament_id_id` |
| `registration_players` | `IX_registration_players_0` | `registration_id` | `PK_registration_players` |
| `tournament_draw_entries` | `IX_tournament_draw_entries_0` | `tournament_id` | `IX_tournament_draw_entries_tournament_id_registration_id` |
| `tournament_matches` | `IX_tournament_matches_0` | `tournament_id` | `UQ_tournament_matches_tournament_id_id` |
| `tournament_match_slots` | `IX_tournament_match_slots_0` | `match_id` | `PK_tournament_match_slots` |
| `tournament_match_slots` | `IX_tournament_match_slots_1` | `tournament_id` | `IX_tournament_match_slots_tournament_id_match_id` |
| `game_participants` | `IX_game_participants_0` | `game_id` | `UQ_game_participants_game_id_user_id` |
| `booking_teams` | `IX_booking_teams_0` | `booking_id` | `PK_booking_teams` |
| `booking_shares` | `IX_booking_shares_0` | `booking_id` | `UQ_booking_shares_booking_id_payer_id` |

## Appendix C. Preservation evidence

- SHA-256 comparison: **254 protected MVC files unchanged**, including Views/layouts, frontend assets, controllers, ViewModels, and SQL. File additions/deletions were also checked in those directories.
- SHA-256 comparison: **4925 files in the original Kicks project unchanged**, excluding Git internals. File additions/deletions were also checked.
- Existing source files modified: **28** (26 entities, DbContext, Program).
- Maintained source/documentation files added: **3** (metadata inspector, offline test, this report).
- No `.csproj`, `.sln`, configuration file, existing SQL/documentation file, or controller was modified. Build-generated `bin`/`obj` artifacts and ignored verification evidence are excluded from the source-change count.
