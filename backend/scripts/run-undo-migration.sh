#!/usr/bin/env bash
# Load backend/.env and run the SQL that removes the AddSystemReferenceMetadata
# migration record so "dotnet ef database update" can re-apply it.
#
# Usage (from repo root or backend/):
#   backend/scripts/run-undo-migration.sh
#
# Then run:
#   dotnet ef database update --project MockHealthSystem.Infrastructure --startup-project MockHealthSystem.Api

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BACKEND_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
ENV_FILE="${BACKEND_DIR}/.env"
SQL_FILE="${SCRIPT_DIR}/undo-migration-AddSystemReferenceMetadata.sql"

if [[ ! -f "$SQL_FILE" ]]; then
  echo "Error: undo-migration-AddSystemReferenceMetadata.sql not found at $SQL_FILE" >&2
  exit 1
fi

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
[[ "$conn" =~ Host=([^;]+) ]]    && export PGHOST="${BASH_REMATCH[1]}"
[[ "$conn" =~ Port=([^;]+) ]]    && export PGPORT="${BASH_REMATCH[1]}"
[[ "$conn" =~ Database=([^;]+) ]] && export PGDATABASE="${BASH_REMATCH[1]}"
[[ "$conn" =~ Username=([^;]+) ]] && export PGUSER="${BASH_REMATCH[1]}"
[[ "$conn" =~ Password=([^;]+) ]] && export PGPASSWORD="${BASH_REMATCH[1]}"

if [[ -z "${PGUSER:-}" || -z "${PGDATABASE:-}" ]]; then
  echo "Error: Could not parse Username and Database from POSTGRES_CONNECTION_STRING." >&2
  exit 1
fi

cd "$BACKEND_DIR"
psql -f "$SQL_FILE"
