#!/usr/bin/env bash
# Apply the AddSystemReferenceMetadata migration by generating its SQL and running it
# with psql, then recording it in __EFMigrationsHistory. Use this when
# "dotnet ef database update" reports "No migrations were applied" but the schema
# is missing the new columns (e.g. ChildBearing on Conditions).
#
# Usage (from repo root or backend/):
#   backend/scripts/apply-system-reference-migration.sh
#
# Requires: dotnet, psql, and backend/.env with POSTGRES_CONNECTION_STRING.

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BACKEND_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
ENV_FILE="${BACKEND_DIR}/.env"
OUTPUT_SQL="${SCRIPT_DIR}/add_system_reference_metadata.sql"

# Load .env (same pattern as init-db.sh)
if [[ -f "$ENV_FILE" ]]; then
  while IFS= read -r line; do
    [[ -z "$line" || "$line" =~ ^[[:space:]]*# ]] && continue
    key="${line%%=*}"
    value="${line#*=}"
    export "$key=$value"
  done < "$ENV_FILE"
fi

if [[ -z "${POSTGRES_CONNECTION_STRING:-}" ]]; then
  echo "Error: POSTGRES_CONNECTION_STRING is not set. Set it in backend/.env (see backend/.env.example)." >&2
  exit 1
fi

# Parse .NET-style connection string into libpq env vars for psql
conn="$POSTGRES_CONNECTION_STRING"
[[ "$conn" =~ Host=([^;]+) ]]     && export PGHOST="${BASH_REMATCH[1]}"
[[ "$conn" =~ Port=([^;]+) ]]     && export PGPORT="${BASH_REMATCH[1]}"
[[ "$conn" =~ Database=([^;]+) ]] && export PGDATABASE="${BASH_REMATCH[1]}"
[[ "$conn" =~ Username=([^;]+) ]] && export PGUSER="${BASH_REMATCH[1]}"
[[ "$conn" =~ Password=([^;]+) ]] && export PGPASSWORD="${BASH_REMATCH[1]}"

if [[ -z "${PGUSER:-}" || -z "${PGDATABASE:-}" ]]; then
  echo "Error: Could not parse Username and Database from POSTGRES_CONNECTION_STRING." >&2
  exit 1
fi

cd "$BACKEND_DIR"

echo "Generating migration SQL..."
dotnet ef migrations script 20260305021213_AddDomainTables 20260312090000_AddSystemReferenceMetadata \
  --project MockHealthSystem.Infrastructure \
  --startup-project MockHealthSystem.Api \
  -o "$OUTPUT_SQL" \
  --idempotent

echo "Applying migration SQL to database ${PGDATABASE}..."
psql -f "$OUTPUT_SQL"

echo "Recording migration in __EFMigrationsHistory..."
psql -v ON_ERROR_STOP=1 -c "INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ('20260312090000_AddSystemReferenceMetadata', '10.0.3') ON CONFLICT (\"MigrationId\") DO NOTHING;"

echo "Done. Restart the API and try again."
