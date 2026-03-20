#!/usr/bin/env bash
# Create the application database and user (PostgreSQL).
# Requires: psql, and superuser access (e.g. postgres or your OS user with trust auth).
# Loads backend/.env and uses the password from POSTGRES_CONNECTION_STRING.
#
# Usage (from repo root or backend/):
#   backend/scripts/init-db.sh
#   backend/scripts/init-db.sh localhost 5432 postgres
#
# If you get "permission denied" when running as yourself, or "peer authentication"
# for user postgres, run only psql as the postgres OS user:
#   USE_SUDO_POSTGRES=1 backend/scripts/init-db.sh

set -e

HOST="${1:-localhost}"
PORT="${2:-5432}"
ADMIN_USER="${3:-postgres}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BACKEND_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
ENV_FILE="${BACKEND_DIR}/.env"
SQL_FILE="${SCRIPT_DIR}/init-db.sql"

if [[ ! -f "$SQL_FILE" ]]; then
  echo "Error: init-db.sql not found at $SQL_FILE" >&2
  exit 1
fi

# Load .env (split only on first '=' so connection string and passwords are preserved)
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

# Extract Password=value from the connection string (password must not contain ';')
if [[ "$POSTGRES_CONNECTION_STRING" =~ Password=([^;]+) ]]; then
  APP_PASSWORD="${BASH_REMATCH[1]}"
else
  echo "Error: Could not find Password= in POSTGRES_CONNECTION_STRING." >&2
  exit 1
fi

# Substitute password into SQL so it works with sudo (psql -v is not reliable through sudo). Escape for SQL (') and for sed (&/\).
ESCAPED_PASSWORD=$(printf '%s' "$APP_PASSWORD" | sed "s/'/''/g")
SAFE_FOR_SED=$(printf '%s' "$ESCAPED_PASSWORD" | sed 's/[&/\]/\\&/g')
TMP_SQL="/tmp/init-db-mockhealthsystem.$$.sql"
sed "s/__APP_PASSWORD__/$SAFE_FOR_SED/g" "$SQL_FILE" > "$TMP_SQL"
trap 'rm -f "$TMP_SQL"' EXIT

echo "Creating database and user (host=$HOST port=$PORT user=$ADMIN_USER) ..."
if [[ -n "${USE_SUDO_POSTGRES:-}" ]]; then
  # Run only psql as postgres. Use Unix socket (no -h) so peer auth applies and no password is needed.
  sudo -u postgres psql -U "$ADMIN_USER" -f "$TMP_SQL"
else
  psql -h "$HOST" -p "$PORT" -U "$ADMIN_USER" -f "$TMP_SQL"
fi
echo "Done. Use POSTGRES_CONNECTION_STRING in backend/.env to connect as mockhealthsystem_user."
