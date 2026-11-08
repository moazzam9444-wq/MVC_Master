# KICKS MVC

Independent ASP.NET Core MVC migration of the KICKS frontend. The original Desktop/Kicks project is untouched.

## Open and run

Open KicksMvc.sln in Visual Studio with .NET 10 support and the ASP.NET workload, select the KicksMvc profile and press F5. Or run:

    dotnet restore
    dotnet run --launch-profile KicksMvc

Home: http://127.0.0.1:5602/index.html
Demo setup: http://127.0.0.1:5602/Pages/Demo/catalogue.html
Database documentation: http://127.0.0.1:5602/docs/database/database.html

Authentication now uses ASP.NET Core Identity and SQL Server. Both player and field-owner registrations require administrator approval. Browser demo accounts/passwords do not authenticate to the MVC application. Other feature demonstrations still use their existing browser storage.

Read [the authentication completion report](docs/backend/AUTHENTICATION-COMPLETE.md) for its architecture and controlled first-administrator provisioning command. The real administrator and approved player are preserved.

Field management is now SQL-backed. Approved owners can publish, edit, archive and restore their own fields, upload a field photograph and select SQL amenities. Public field cards, details and homepage highlights read real eligible fields. See [the Phase 2 completion report](docs/backend/FIELD-MANAGEMENT-COMPLETE.md) for testing, manual steps and limitations. No sample fields are seeded; an empty catalogue is expected until an owner publishes a field.

## Structure

- Controllers: real MVC actions for all 20 migrated views. Existing .html URLs are preserved as controller routes, including query strings and hashes.
- Views: compiled Razor views. Full markup and DOM hooks are preserved; no default Bootstrap layout is injected.
- wwwroot: original CSS, JavaScript, images, fonts and vendor assets.
- ViewModels: dedicated validated authentication/review inputs and presentation models.
- Services/Authentication: account access checks, private ownership-document storage and explicit development administrator provisioning.
- Models/Entities: 27 domain entities, including the integer-key Identity User. Models/Legacy retains the existing meetings table.
- Data/KicksDbContext.cs: SQL Server EF Core mapping, including composite keys, constraints, indexes and NO ACTION relationships.
- Services/Contracts: boundary for the next backend implementation.
- Data/Migrations: authoritative Code First baseline, upgrade, and model snapshot. database/KICKS-schema.sql and docs/database retain the earlier design reference.
- tests and scripts/test-regressions.cjs: migrated regression harness for MVC-hosted pages and web-root assets.
- docs/MVC-MIGRATION.md: scope, verification and backend work remaining.

## Database readiness

The existing localhost/Kicks database has been upgraded using a verified existing-schema baseline and a separate Identity/domain migration. The development configuration uses Windows Authentication through ConnectionStrings:DefaultConnection. Other environments must provide that setting, for example through ConnectionStrings__DefaultConnection. Database context and Identity infrastructure are registered; startup never creates a database, applies migrations, or creates accounts.

The database has 27 current domain tables, 1 retained legacy meetings table, 6 Identity support tables, and the EF history table (35 total). Identity uses the existing dbo.users integer identity key. Its three seeded role definitions are player, owner and admin; no users are seeded.

Read the completed database setup report in docs/backend/DATABASE-SETUP-COMPLETE.md before changing migrations. Never run the old KICKS-schema.sql or the initial baseline CREATE operations against this existing database. The one-time baseline-adoption script is already applied.

Restore the pinned EF tool with `dotnet tool restore`. Normal future changes use reviewed EF migrations; the current database is already up to date.

Validate the EF model without connecting to a database:

    dotnet run --no-launch-profile -- --verify-model

Registration, login, logout, account review/status and dashboard authorization use server-side Identity. Role membership is authoritative; Pending users have no assigned role and cannot sign in. Every authenticated request rechecks security stamps, current roles, lockout and Active status. Ownership proofs are private files under App_Data/ownership with SQL metadata and administrator-only downloads.

Booking and field availability use SQL Server: exactly 1- or 2-hour sessions within 13:00–23:00 Amman time, server-priced confirmation, interval overlap prevention, player/owner dashboards and history. One-hour starts run 13:00–22:00; two-hour starts run 13:00–21:00. Players may cancel their own Confirmed reservation with at least three hours remaining; owners may cancel future own-field reservations and complete ended sessions. Confirmed means reserved, with no payment collected and settlement deferred. See the [Phase 3 report](docs/backend/BOOKING-COMPLETE.md) and [duration/opening-hours enhancement](docs/backend/BOOKING-DURATION-ENHANCEMENT.md).

Mutating SQL/browser suites now require `KICKS_TEST_CONNECTION` explicitly targeting a separately restored local database named `Kicks_BookingTest_<yyyyMMdd>_<8 lowercase hexadecimal characters>`. They refuse the original `Kicks` database. Authentication browser suites also require `KICKS_TEST_URL` for the isolated application and reject the normal port 5602. Set these only in the test process; keep normal development configuration pointed at `Kicks`.

Teams management now uses SQL Server through the existing Teams page: player-created squads, captain membership, profiles, editing, join requests, approval/rejection, departure/removal, captain transfer and private logo storage. Each player belongs to at most one team, matching the existing schema. See [Phase 4 completion and verification](docs/backend/TEAMS-COMPLETE.md).

Tournaments, open games, challenges, messages and payments retain their separate prototype workflows for later phases. Migrated reservations and Teams management never write prototype booking/team persistence or fake payments. Profile editing/password changes remain deferred.

Field amenities were initialized explicitly once with six reference choices. For another development environment with the same reviewed schema, the idempotent reference-data command is `dotnet run --project KicksMvc.csproj --launch-profile KicksMvc -- --initialize-field-amenities`. Ordinary startup never runs this command or changes the schema. Back up `App_Data/field-images` with SQL; these public venue images are separate from private ownership documents.

Run the SQL-backed integration suite with `dotnet run --project tests/Auth.Integration/Auth.Integration.csproj --configuration Release`. It creates uniquely marked test users, exercises real Identity/SQL operations and removes only its own records. Administrator review uses a synthetic principal confined to the in-memory test host; it never creates an administrator account or role membership. Browser verification instructions are in the completion report.

The original GitHub repository is not changed or pushed by this migration.
