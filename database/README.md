# Database artifacts

The current schema is owned by EF Core Code First in `Data/KicksDbContext.cs` and `Data/Migrations`.

- `KICKS-schema.sql` is the preserved earlier design reference. Do not execute it to recreate or upgrade the existing database. The browser schema documentation also describes that earlier design.
- `migrations/AdoptExistingKicksBaseline.sql` is a guarded, machine-specific, one-time adoption script for the inspected legacy database. It has already been applied. It does not execute the baseline migration's table creation operations. Do not rerun it.
- `migrations/UpgradeKicksIdentityAndDomain.sql` is the reviewed SQL generated from the baseline to the applied upgrade. It is an audit/reference artifact, not a script to rerun against the upgraded database.

The normal future workflow is to change the EF model, generate a new migration, review its SQL and data-preservation implications, back up the database, and apply the approved migration. Do not edit already-applied migration files or insert migration-history rows for a schema that has not been verified.

See the detailed completion report at `docs/backend/DATABASE-SETUP-COMPLETE.md`.
