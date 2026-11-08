# KICKS — Existing database upgrade completion report

Date: 7 November 2026, Asia/Amman. Target: Windows-authenticated SQL Server `localhost`, existing database `Kicks`.

**Result: successfully upgraded the existing database in place.** The original database, all 26 original tables, all 58 original foreign-key objects, and all 21 original foreign keys referencing `users` were preserved. The application builds and the 20 existing MVC pages still render. No login/register endpoints, CRUD, business services, sample users, or administrator accounts were implemented.

This report supersedes the readiness/approval status in the earlier EF preparation report. The earlier report and original SQL remain historical design references.

## 1. Backup and preflight

An exact `COUNT_BIG(*)` was obtained for each of the 26 existing tables before any schema/history change. Every table, including `users`, contained **0 records**. This was not inferred from approximate SQL metadata.

Verified backup:

`C:\Program Files\Microsoft SQL Server\MSSQL17.MSSQLSERVER\MSSQL\Backup\Kicks_before_identity_20261107_2d30387edba84139b4291bde225cd768.bak`

- SQL Server full **COPY_ONLY** backup with compression and **CHECKSUM**.
- Logical backup size: 7,888,896 bytes; compressed backup size: 726,726 bytes.
- SQL Server `RESTORE VERIFYONLY ... WITH CHECKSUM`: **the backup set is valid**.
- Server-side file-existence verification: **file exists**.
- Verification was repeated in the adoption script before changing migration history.
- The SQL service backup directory is ACL-restricted to the desktop account. Filesystem inspection from that account was denied; file existence, backup metadata and readability were verified through SQL Server instead. No filesystem hash or trial restore is claimed.
- The database schema and counts were re-inspected immediately before adoption and matched the captured pre-backup state.

No replacement database, table recreation, database reset, or execution of the original `KICKS-schema.sql` occurred.

## 2. Migrations and baseline architecture

| Migration | Purpose | Execution |
|---|---|---|
| `20261107152628_BaselineExistingKicks` | Reverse-engineered the actual existing 26-table schema, including its 207 columns, 58 FKs, 56 checks, keys, indexes, defaults, computed columns, and retained meetings table. | Existing schema was adopted with a guarded migration-history insert. Its CREATE TABLE operations were **not executed** against existing tables. |
| `20261107153123_UpgradeKicksIdentityAndDomain` | Upgrade from that accurate baseline to the current domain/Identity model. | Applied successfully through `dotnet ef database update`. |

Both rows are present in `dbo.__EFMigrationsHistory` with EF product version `10.0.14`.

The baseline was generated from live catalog metadata, not from the newer SQL design or an empty migration pretending that the current model already existed. A temporary catalog-model context was used for scaffolding and removed; the checked-in migration designer and final snapshot belong to `KicksDbContext`. The baseline migration contains full legacy creation operations for audit/provisioning purposes, but the existing database was adopted rather than recreated.

Adoption and upgrade SQL were reviewed before execution. The adoption script verifies the database and users-table object IDs, expected table/FK counts, absence of migration history, the 21 original user FK object IDs, and table emptiness. The upgrade starts with transactional exclusive locks and checks on all 26 original tables. If records had appeared, it would have thrown before any schema operation.

Reviewed upgrade operations:

- **8 CREATE TABLE operations**: 2 missing domain tables and 6 Identity support tables.
- **No DROP TABLE, DROP PRIMARY KEY, or DROP FOREIGN KEY operations in the upgrade's Up path.**
- Four retired columns removed from the verified-empty users table: custom password salt, algorithm, iterations, and the old role column.
- Three persisted computed end columns regenerated with the retained expressions.
- Other domain columns, constraints, defaults and indexes brought into line with the current reviewed model.
- Existing user relationships retained their original names/object IDs where earlier naming suffixes differed from the newer design.

EF scaffolding warned about possible data loss because of the retired columns/type/nullability changes. That warning was reviewed, not suppressed. The exact all-table emptiness checks, verified backup, reviewed SQL, and transaction guards established that no populated data was removed. The generated Down path was not executed; any future rollback requires its own backup and data-loss review.

## 3. Table counts and preserved data

| Category | Before | After |
|---|---:|---:|
| Current domain tables | 25 of the intended 27 | 27 |
| Retained legacy `meetings` | 1 | 1 |
| Identity support tables | 0 | 6 |
| EF migration-history table | 0 | 1 |
| **Total physical tables** | **26** | **35** |
| Mapped EF tables | — | 34 |
| Foreign-key constraints | 58 | 76 |
| CHECK constraints | 56 | 78 |

The 34 mapped tables have 269 columns. The physical database has 271 columns when the history table's two columns are included.

Preservation verification confirmed:

- The same database ID and all 26 original table object IDs remain.
- `dbo.users` object ID remains **1221579390**; its existing integer identity primary key remains in place.
- All **58** original foreign-key object IDs and their column relationships remain, including all **21** original relationships to `users`.
- All original table row counts remain 0; no existing records were lost.
- `meetings` remains with its original table object ID, schema, constraints and relationships. `LegacyMeeting` is storage compatibility only; no meeting feature was restored.
- The only new data is **three role definitions** and **two migration-history rows**. `users` still has **0 records**.

## 4. Identity architecture

- [User](C:/Users/USER/Desktop/KicksMvc/Models/Entities/User.cs) derives from `IdentityUser<int>` and maps to the existing `dbo.users` table.
- `Id` remains the existing integer identity. There is no `AspNetUsers` table or separate domain-account identity.
- [KicksDbContext](C:/Users/USER/Desktop/KicksMvc/Data/KicksDbContext.cs) derives from `IdentityDbContext<User, IdentityRole<int>, int>`.
- Identity schema version 2 is explicitly selected in both context and registration: standard password/role support, without an unused passkey table.
- All Identity FK deletes are `NO ACTION`, consistent with the domain's history-preserving relationships.
- `PasswordHash` is the inherited Identity field mapped to `password_hash` with standard Identity hashing infrastructure. No browser hashes, legacy credentials, or accounts were imported.
- The obsolete salt/algorithm/iteration fields were removed because there were no stored users or passwords to migrate. The old password-iteration CHECK was retired with that custom password format.
- Identity role membership is the sole authorization-role source. The old `users.role` column was removed rather than maintaining a competing permission field.
- Integer-key role definitions: **1 player**, **2 owner**, **3 admin**. These are roles, not user accounts. A role-name CHECK preserves the allowed role concepts.
- A unique index on `AspNetUserRoles.UserId` preserves the existing one-global-role-per-account policy. A future role change must remove/add membership atomically rather than add a second independent role.
- Approval fields `status`, `requested_role`, `reviewed_by`, `reviewed_at`, and their relationships/status checks remain. An account's approval state is separate from its Identity role.
- Identity concurrency/security stamp columns and standard normalized-name/email indexes are configured. Existing email uniqueness is retained and normalized email is also unique when supplied.

Identity support tables are `AspNetRoles`, `AspNetUserRoles`, `AspNetUserClaims`, `AspNetRoleClaims`, `AspNetUserLogins`, and `AspNetUserTokens`.

Identity managers, stores, cookies, default token providers, and middleware are registered as infrastructure. No Identity UI, API endpoints, or database-backed forms were added. The frontend still uses its existing browser accounts/storage.

## 5. Domain changes

- Added `team_recruiting_positions` and `booking_shares` with the reviewed keys, checks, indexes, and relationships.
- Added `users.address` and the required standard Identity user columns.
- Added missing domain audit/booking/payment fields and corrected schema rules according to the existing EF model.
- Preserved the 12 uniqueness-only index conversions and 3 composite alternate keys from the EF preparation pass.
- Preserved the 73 domain navigation properties, 68 current domain FKs, 75 remaining domain checks after the two approved obsolete user checks were retired, all 15 domain uniqueness guarantees, and all 60 original design index access paths/filtered rules.
- Preserved literal defaults/explicit zero-false behavior, server timestamps, decimal types and DateTimeOffset mappings, computed time expressions, and the booking Amman offset/hour CHECK.

SQL Server reports the three DATEADD computed end columns as nullable in catalog metadata even though their non-null inputs determine their values. Their persisted expressions match the model; no claim is made that SQL Server now marks these computed columns NOT NULL. All ordinary mapped column types and nullability were verified against the live database. EF's additive Identity migration also supplied normal zero/false defaults for the five new non-null integer/boolean user fields; these agree with their CLR values.

## 6. Connection and startup

- Connection name: **DefaultConnection**.
- Development configuration: `localhost` / `Kicks`, Windows Authentication, trusted development server certificate, exactly as requested.
- Other environments must supply `ConnectionStrings:DefaultConnection`, for example via `ConnectionStrings__DefaultConnection`.
- The context is registered normally; the old `Database:EnableContext`/`ConnectionStrings:Kicks` switch was removed.
- The [design-time factory](C:/Users/USER/Desktop/KicksMvc/Data/KicksDbContextFactory.cs) uses the same configuration name and context. It does not create or migrate a database by constructing the context.
- Startup never calls `EnsureCreated`, `Migrate`, a database reset, or user seeding.

## 7. Verification performed

| Check | Result |
|---|---|
| Solution Release build | PASS, 0 warnings, 0 errors |
| Offline domain/Identity model validation | PASS |
| In-memory change-tracker behavior | 22 checks passed; no database test accounts |
| Live SQL schema vs finalized EF metadata | PASS: all tables, ordinary column types/nullability, keys, FKs, indexes, CHECK expressions, configured defaults and computed expressions |
| Original table/record/FK preservation | PASS: all 26 tables, all 58 FKs, all 21 original user FKs |
| Constraints enabled/trusted | All FK and CHECK constraints enabled and trusted |
| No duplicate user table | PASS; no AspNetUsers |
| EF history | Both intended migrations applied; no pending migration |
| Pending model changes | None |
| Future migration generation | Generated an empty `VerifyFutureMigrationPipeline` migration, inspected empty Up/Down, then removed only that unapplied probe through EF tooling |
| MVC pages | All 20 routes returned HTTP 200 and HTML |
| Static JavaScript | Four key scripts returned HTTP 200 |
| Protected existing MVC files | 254 files unchanged by SHA-256, including original SQL, Views/layouts, frontend assets, controllers and ViewModels |
| Original frontend project | 4,925 existing files unchanged by SHA-256 |

No browser workflow was replaced or connected to SQL as part of these checks. No SQL test user, booking, payment, team or tournament was inserted. Runtime CRUD/concurrency/authentication flows are not claimed as tested.

Reproducible checks from the project folder:

```powershell
dotnet tool restore
dotnet build KicksMvc.sln --no-restore --configuration Release
node tests/ef-model-preparation-check.cjs
node tests/database-setup-check.cjs
dotnet ef migrations has-pending-model-changes --context KicksDbContext --configuration Release --no-build
dotnet ef migrations list --context KicksDbContext --configuration Release --no-build
```

`database-setup-check.cjs` reads the live database and compares it with the model and locally captured before-state; it never writes database records. Its preservation assertions intentionally describe this initial empty-database setup, not a later populated production system.

## 8. Files changed

Modified existing files:

- [KicksMvc.csproj](C:/Users/USER/Desktop/KicksMvc/KicksMvc.csproj): Identity EF package, version 10.0.14 matching EF SQL Server/Design.
- [Program.cs](C:/Users/USER/Desktop/KicksMvc/Program.cs): normal DbContext/Identity infrastructure registration and authentication middleware, no endpoints or auto-migration.
- [appsettings.json](C:/Users/USER/Desktop/KicksMvc/appsettings.json), [development settings](C:/Users/USER/Desktop/KicksMvc/appsettings.Development.json): replace the old enable switch with DefaultConnection configuration.
- [User.cs](C:/Users/USER/Desktop/KicksMvc/Models/Entities/User.cs): inherit IdentityUser<int>, retire duplicate/custom credential/role properties, use inherited PhoneNumber mapped to existing phone column.
- [KicksDbContext.cs](C:/Users/USER/Desktop/KicksMvc/Data/KicksDbContext.cs): Identity base model, retained domain rules, legacy meetings mapping, role definitions and preserved physical FK names.
- [KicksModelInspection.cs](C:/Users/USER/Desktop/KicksMvc/Data/Diagnostics/KicksModelInspection.cs): report mapped DbSets only and Identity concurrency metadata.
- [ef-model-preparation-check.cjs](C:/Users/USER/Desktop/KicksMvc/tests/ef-model-preparation-check.cjs): retain domain regression checks with explicit approved Identity transition exceptions.
- [README.md](C:/Users/USER/Desktop/KicksMvc/README.md): current connection/schema status and migration usage.

Added maintained files:

- [dotnet-tools.json](C:/Users/USER/Desktop/KicksMvc/dotnet-tools.json): local EF tool pinned to 10.0.14.
- [KicksDbContextFactory.cs](C:/Users/USER/Desktop/KicksMvc/Data/KicksDbContextFactory.cs).
- [KicksRoles.cs](C:/Users/USER/Desktop/KicksMvc/Models/Identity/KicksRoles.cs), [LegacyMeeting.cs](C:/Users/USER/Desktop/KicksMvc/Models/Legacy/LegacyMeeting.cs).
- [Baseline migration](C:/Users/USER/Desktop/KicksMvc/Data/Migrations/20261107152628_BaselineExistingKicks.cs) and its designer.
- [Upgrade migration](C:/Users/USER/Desktop/KicksMvc/Data/Migrations/20261107153123_UpgradeKicksIdentityAndDomain.cs) and its designer.
- [Final model snapshot](C:/Users/USER/Desktop/KicksMvc/Data/Migrations/KicksDbContextModelSnapshot.cs).
- [Read-only SQL inspection script](C:/Users/USER/Desktop/KicksMvc/scripts/inspect-kicks-database.ps1).
- [Live schema verification](C:/Users/USER/Desktop/KicksMvc/tests/database-setup-check.cjs).
- [One-time baseline adoption SQL](C:/Users/USER/Desktop/KicksMvc/database/migrations/AdoptExistingKicksBaseline.sql), [reviewed upgrade SQL](C:/Users/USER/Desktop/KicksMvc/database/migrations/UpgradeKicksIdentityAndDomain.sql), [database artifact guide](C:/Users/USER/Desktop/KicksMvc/database/README.md).
- This report. Ignored evidence/logs are under `.local/database-setup/`.

No Razor View, layout, CSS, JavaScript, asset, existing controller, ViewModel, or original frontend file was modified. The original SQL script was preserved and never executed.

## 9. Next stage and remaining limitations

**Database setup is complete; no further database-creation command is required.** Future model changes should use a new reviewed migration and backup; do not reapply the baseline, adoption script, upgrade reference SQL, or original schema script.

Before connecting application features:

1. Implement server login/registration and enforce approval/Active status during sign-in and protected operations. Standard Identity registration alone does not enforce KICKS approval rules.
2. Provision a real administrator through a separately authorized controlled process; none exists in SQL today.
3. Use UserManager/RoleManager and DTOs; do not import the browser's demo hashes or trust browser role/password/payment fields.
4. Implement transactional capacity, venue/team scheduling, split payment/expiry/refund, idempotency and bracket commands. Identity stamps do not solve domain concurrency.
5. Replace browser persistence feature by feature while preserving the existing interfaces. Group-stage behavior and real payment integration still require their separately reviewed scope.

These are intentionally unimplemented backend features, not failures of the completed schema migration.
