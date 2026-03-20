#!/usr/bin/env bash
# Run dotnet ef without HTTP/HTTPS proxy (avoids 403 when a proxy is set by IDE/env).
# Usage: from repo root: backend/scripts/run-ef.sh database update
#        or: backend/scripts/run-ef.sh migrations add MigrationName
# From backend/: ./scripts/run-ef.sh database update
set -e
cd "$(dirname "$0")/.."
export HTTP_PROXY=
export HTTPS_PROXY=
exec dotnet ef "$@" --project MockHealthSystem.Infrastructure --startup-project MockHealthSystem.Api
