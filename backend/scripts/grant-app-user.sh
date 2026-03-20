#!/usr/bin/env bash
# Grant the app user (from .env) full rights on all objects in the app database.
# Use this if you see "permission denied for table ..." (e.g. after migrations were run as postgres).
# Requires: same as init-db.sh (psql, sudo to postgres for localhost).
#
# Usage (from repo root or backend/):
#   backend/scripts/grant-app-user.sh

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BACKEND_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
ENV_FILE="${BACKEND_DIR}/.env"
SQL_FILE="${SCRIPT_DIR}/grant-app-user.sql"

if [[ ! -f "$SQL_FILE" ]]; then
  echo "Error: grant-app-user.sql not found at $SQL_FILE" >&2
  exit 1
fi

# Load .env
if [[ -f "$ENV_FILE" ]]; then
  while IFS= read -r line; do
    [[ -z "$line" || "$line" =~ ^[[:space:]]*# ]] && continue
    key="${line%%=*}"
    value="${line#*=}"
    export "$key=$value"
  done < "$ENV_FILE"
fi

if [[ -z "${POSTGRES_CONNECTION_STRING:-}" ]]; then
  echo "Error: POSTGRES_CONNECTION_STRING is not set. Set it in backend/.env." >&2
  exit 1
fi

if [[ "$POSTGRES_CONNECTION_STRING" =~ Database=([^;]+) ]]; then
  APP_DATABASE="${BASH_REMATCH[1]}"
else
  echo "Error: Could not find Database= in POSTGRES_CONNECTION_STRING." >&2
  exit 1
fi
if [[ "$POSTGRES_CONNECTION_STRING" =~ Username=([^;]+) ]]; then
  APP_USER="${BASH_REMATCH[1]}"
else
  echo "Error: Could not find Username= in POSTGRES_CONNECTION_STRING." >&2
  exit 1
fi

# Escape for sed
SAFE_USER=$(printf '%s' "$APP_USER" | sed 's/[&/\]/\\&/g')

TMP_SQL="/tmp/grant-app-user.$$.sql"
sed "s/__APP_USER__/$SAFE_USER/g" "$SQL_FILE" > "$TMP_SQL"
trap 'rm -f "$TMP_SQL"' EXIT

echo "Granting privileges on database \"$APP_DATABASE\" to user \"$APP_USER\" ..."
if sudo -u postgres psql -d "$APP_DATABASE" -f "$TMP_SQL"; then
  echo "Done. The app user $APP_USER should now have full access to all tables."
else
  echo "Failed. Check that database \"$APP_DATABASE\" exists and you can run psql as postgres." >&2
  exit 1
fi
