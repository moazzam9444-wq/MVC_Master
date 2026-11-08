# KICKS Backend Phase 2 — Field Management completion report

**Implemented and verified: 225 automated assertions passed, 0 failed in the final runs.** The Release solution builds with zero warnings/errors. No EF migration, model change, schema reset or Backend Phase 3 implementation was performed.

## Product rules

The existing schema has an archival flag rather than a field approval/payment state. The product owner explicitly selected **publish on save for approved owners; defer listing fees**. The module therefore never creates a payment or silently charges the configured prototype listing fee. Only an Active Identity owner can manage fields. Public visibility requires a non-archived field whose account is Active and still has owner Identity membership.

The old browser Delete action is replaced by safe Archive/Restore. Archival retains the field, images, amenities and all historical relationships; no physical-delete endpoint exists. Restoring republishes the owner's own field. No administrator-controlled field approval status exists in this schema. Owners cannot change ownership, rating, creation timestamps or account approval.

## Backend and SQL integration

`FieldService` uses the existing `KicksDbContext`, DefaultConnection, integer field/user keys, `fields`, `field_images`, `amenities` and `field_amenities`. Dedicated input/query/display ViewModels prevent entity binding and private-account serialization. Owner IDs come from the authenticated NameIdentifier claim. The existing Identity cookie/status checks remain intact; management endpoints additionally require role owner.

Create/edit validate required name, city, area and description; schema-aligned text lengths; description length 10–5,000; formats 5v5/6v6/7v7; non-negative decimal pricing up to the existing decimal(12,2) limit with at most two fractional digits; and valid amenity IDs. Duplicate selected amenities are collapsed. EF queries are parameterized, with fixed allow-listed sort options.

Updates and archival first lock the authenticated owner's target row in a serializable transaction. A protected aggregate fingerprint covers editable/protected field values, image rows and selected amenities. An outdated/tampered version returns HTTP 409 instead of overwriting newer data. This provides concurrency handling without altering the schema. New-field responses use the saved entity ID directly, avoiding a race with another creation. Missing/foreign-owner targets return 404; unauthorized roles challenge/deny through standard Identity authorization. All unsafe MVC requests retain global anti-forgery validation.

Fields have no capacity column or exact-address column, so no new columns were invented. Location uses City and Area. New/moved fields have unknown coordinate precision and no fabricated coordinates; unrelated edits preserve existing coordinates. Rating is read-only and starts at the existing database/CLR default.

## Owner dashboard

Owner Dashboard → Fields now fetches a SQL Razor partial, scoped to the current owner, with twelve records per page, empty state, real photos, name/location/format, price, publication state and Add/Edit/View/Archive/Restore actions. The existing dashboard classes, dialogs, typography and colors are reused. The editor reads current SQL values, populates amenity checkboxes and preserves the current image unless replacement/removal is explicit. Refresh and a fresh browser session retrieve persisted records. Owner overview field highlights use a bounded SQL snapshot and the SQL total; unrelated business panels retain their prototype behavior.

The admin Fields tab links to the public catalogue and does not expose owner mutation controls. This phase does not create a second administrator field-editing role or endpoint.

## Public catalogue, details and homepage

- Existing .html routes are retained. `/api/fields` supplies at most twelve DTOs per page and a total count, with server-side search over name/city/area, city/format/price/rating filters, newest/price/rating sort and validated paging. Pagination controls render a small window of page links.
- Public location choices come from an eligible SQL city query bounded to 100 distinct choices, supplementing the existing catalogue options.
- Catalogue card markup/design is retained. Field details resolve the SQL ID, display actual amenities/price/location and serve the field photo. Owner identity/contact information and private ownership documents are not exposed.
- Homepage field highlights and location search query SQL, bounded to twelve results. Its other activity cards still use their existing prototype data.
- Availability/scheduling filters and booking/payment controls for SQL fields are disabled with an explicit later-phase message. No scheduling engine or booking/payment write was added. Prototype field IDs must not be treated as SQL booking records.

SQL is authoritative for the migrated owner field tab, catalogue, detail page and homepage field highlights. Browser field CRUD and demo records cannot change these server records. Shared storage/code is retained for unfinished modules, including their discovery, activity and business prototypes; those modules have not been certified as SQL-backed by this phase.

## Images

The existing single-photo UI accepts an optional JPG/JPEG, PNG or WebP up to 5 MB. The server validates actual size, extension/MIME agreement and image signatures/container markers. Generated GUID filenames prevent path traversal and collisions. No SVG/HTML or browser-supplied storage path is accepted.

Image bytes live under `App_Data/field-images`, separately from `App_Data/ownership`. SQL image rows store the generated key and field/order relationship. `/media/fields/{key}` validates the key and SQL visibility before delivery, sets the image MIME, nosniff, a restrictive CSP and no-store. Eligible public images are accessible anonymously; archived images are accessible only to their authenticated owner. Private ownership documents are never delivered by this route.

Replacement/removal and database changes share a transaction. Existing unique image sort-order entries are removed/flushed within that transaction before replacement. A failed database operation removes the newly created file; obsolete files are removed after commit. A storage-cleanup failure is logged without exposing keys/tokens. Unrelated field edits preserve photos. Fields without an uploaded photo use an explicitly identified illustrative fallback image.

Limitations: the UI manages one primary photo, matching the previous uploader. There is no gallery reorder UI, image re-encoding, malware-scanning service or cloud object storage. File-signature validation does not claim to scan malware. Back up public image files with SQL and retain durable Data Protection keys for edit-version/cookie continuity. A process crash between filesystem and SQL commit can leave an inaccessible orphan file requiring maintenance.

## Amenities

Owners select SQL amenity IDs; the server checks them and updates the join table atomically without duplicate relationships. The catalogue/details read those relationships. The previously empty amenity dictionary was explicitly initialized with **Floodlights, Parking, Changing rooms, Showers, Seating and Water**. These six intentional reference rows remain available for real use. No sample user or field remains.

The Development-only `--initialize-field-amenities` command adds missing reference names; it does not migrate the database or create users, and it is not invoked on ordinary startup. Existing reference names/data are retained.

## Executed verification

| Final suite | Passed | Failed |
|---|---:|---:|
| SQL-backed field integration | 52 | 0 |
| Actual MVC field browser workflows, Microsoft Edge | 33 | 0 |
| Existing authentication integration regression | 84 | 0 |
| Existing authentication browser regression | 56 | 0 |
| **Total** | **225** | **0** |

Additional checks: Release solution build; offline EF SQL/model parity; EF no-pending-model-change check; full before/after SQL catalog comparison; database table-content SHA-256 comparison; original frontend inventory/hashes; desktop/tablet/mobile screenshot inspection.

Field tests exercised creation and SQL persistence, reload and fresh-context persistence, My Fields isolation, edit prepopulation/update, valid image upload/display/replacement/removal, spoofed/oversized rejection, amenity persistence/update/unknown-ID rejection, catalogue/detail/search/city/price/format/sort/paging, twelve-result bounds, archive/restore and visibility, anonymous/player mutation denial, foreign-owner GET/edit/archive denial, forged OwnerId/rating/status input, missing CSRF, invalid values, stale edits, inactive-owner cookie revocation and hidden public fields. Browser layouts were checked at 1440, 768 and 390 pixels for owner list/editor and public catalogue, with no horizontal overflow or JavaScript runtime errors.

The authentication regressions exercised the existing registration/review/login/logout/status/lockout/CSRF/throttling behaviors. Administrator review used the existing isolated test-host principal; the real administrator password was neither requested nor used. Temporary approved owner/player accounts were created only by the excluded test fixture assembly and removed in cleanup.

During development, compile-time mistakes in the test harness were corrected before its first successful integration run. An initial browser run reached the final logout check but targeted a sidebar button hidden at mobile width; the harness now checks that control at desktop width. Code review also caught and corrected a potential saved-ID response race and a homepage renderer-name mismatch. Image replacement was explicitly tested against the existing unique sort-order constraint. No failing assertion remains in the final runs.

## Preservation

- Schema/catalog is unchanged: **35 physical tables, 76 foreign keys**, same table/constraint object IDs and mappings, with no pending model change.
- SHA-256 comparison of ordered SQL table contents matches for every table except the intentionally initialized amenity dictionary. That dictionary began empty and now contains exactly six reference rows.
- The real administrator, manually approved player, their complete Identity/account data, all existing roles, migration history and unrelated records are unchanged.
- Temporary users, test fields, images and join rows were removed by unique fixture markers/owner IDs only. The public-image directory has no remaining test files; private ownership documents are preserved. No identity counter was reset; ordinary gaps from removed test records remain.
- All **4,925 original frontend files** under `C:/Users/USER/Desktop/Kicks` retain their original hashes/inventory.

Evidence is under `.local/field-phase/`: final suite logs, model check, schema/data snapshots, preservation JSON and screenshots. Data snapshots contain content digests and safe metadata, not exported password hashes or plaintext credentials. The temporary application on port 5615 was stopped after testing; the user's original running application was preserved.

## Files

Created:

- `Controllers/OwnerFieldsController.cs`.
- `ViewModels/FieldViewModels.cs`.
- `Services/Fields/FieldService.cs`, `Services/Fields/FieldImageStore.cs`.
- `Views/Shared/_OwnerFields.cshtml`.
- `wwwroot/js/server-fields.js`, `wwwroot/css/server-fields.css`.
- `tests/Auth.Integration/FieldTests.cs`, `tests/field-browser.cjs`.
- This report.

Modified:

- `Program.cs`: field service registration and explicit reference-data initializer; existing authentication settings retained.
- `Controllers/FieldsController.cs`: bounded public JSON/city/detail/image routes, retained existing page routes.
- `Views/Dashboard/Owner.cshtml`, `Views/Fields/Index.cshtml`, `Views/Fields/Details.cshtml`: minimum asset/filter wiring.
- `wwwroot/js/dashboard.js`, `dashboard-overview.js`, `account.js`, `fields.js`, `field-details.js`, `opendesign-home.js`: scoped SQL field integration; existing shared prototypes retained for other modules.
- `tests/Auth.Integration/Program.cs`: explicit field test/fixture modes only, excluded from application compilation.
- `README.md`.

No entity, DbContext, migration, database-creation script, Identity role definition or authentication service implementation was modified.

## Manual testing

1. Restart the MVC app to load the rebuilt Razor/JavaScript integration: `dotnet run --project KicksMvc.csproj --launch-profile KicksMvc`.
2. Use an approved Active owner account. If needed, register an owner with ownership proof and approve it using the existing real administrator flow. No owner is auto-provisioned by startup.
3. Open Owner Dashboard → Fields → Add field. Enter name, city, area, description, format and price. Choose facilities and optionally attach a supported photo. Save publishes immediately without checkout.
4. Refresh, then open a fresh browser session and sign in again. Confirm the field persists. Edit price/amenities, save and check the public catalogue/detail page.
5. Try an invalid image and missing/negative values. They must show errors without creating/updating a field. Edit an old copy in another tab after saving a newer change; it must report a conflict.
6. Archive the field, confirm its public detail is unavailable, and restore it. History remains stored. Replace/remove the photo explicitly and confirm public rendering updates.
7. Test with another owner/player/anonymous context: management and foreign-owner edits must be denied. Existing real accounts do not need to be edited for these checks.

Reproduce automated verification from the project folder:

```powershell
dotnet build KicksMvc.sln --configuration Release
dotnet run --project tests/Auth.Integration/Auth.Integration.csproj --configuration Release -- --fields
node tests/field-browser.cjs
dotnet run --project tests/Auth.Integration/Auth.Integration.csproj --configuration Release
node tests/ef-model-preparation-check.cjs
dotnet ef migrations has-pending-model-changes --context KicksDbContext --configuration Release --no-build
```

`field-browser.cjs` starts/stops the built MVC app on loopback port 5615 and cleans up only its own fixtures. It requires Playwright and Microsoft Edge. The older database-setup script asserts the original all-empty setup and is not the preservation validator for this intentionally populated database.

## Remaining scope and recommended Phase 3

Field management is complete for the current single-image UI and schema. Unfinished booking, payment, team, game, tournament, challenge, messaging/discovery and feedback modules remain prototypes. Availability/nearby-distance scheduling is not implemented for SQL fields; city/area search and map search links are supported. Location choices are bounded to 100 distinct cities and owner overview highlights to the first twelve fields.

Recommended Phase 3: SQL-backed booking availability/reservations with server-authoritative pricing, ownership/resource checks and a reviewed transaction/locking protocol for venue time conflicts. Payment implementation should follow its own approved design. No Phase 3 logic was added here.
