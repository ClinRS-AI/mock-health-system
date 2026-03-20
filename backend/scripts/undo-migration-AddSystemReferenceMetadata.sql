-- Run this only if the migration "AddSystemReferenceMetadata" is recorded as applied
-- but the schema changes were never applied (e.g. DB restored from backup).
--
-- Easiest: from repo root, run:
--   backend/scripts/run-undo-migration.sh
-- then: dotnet ef database update --project MockHealthSystem.Infrastructure --startup-project MockHealthSystem.Api
--
-- Or load backend/.env yourself, parse POSTGRES_CONNECTION_STRING into PGHOST, PGPORT, PGDATABASE, PGUSER, PGPASSWORD, and run:
--   psql -f backend/scripts/undo-migration-AddSystemReferenceMetadata.sql

DELETE FROM "__EFMigrationsHistory"
WHERE "MigrationId" = '20260312090000_AddSystemReferenceMetadata';
