#!/usr/bin/env bash
set -euo pipefail

# Sync VITE_API_BASE_URL on the Render frontend static site and trigger a redeploy.
#
# Required env vars:
#   RENDER_API_KEY
#   RENDER_FRONTEND_SERVICE_ID
#   API_BASE_URL
#
# Optional:
#   RENDER_API_BASE_URL (default: https://api.render.com/v1)
#   TRIGGER_DEPLOY      (default: true)

RENDER_API_BASE_URL="${RENDER_API_BASE_URL:-https://api.render.com/v1}"
TRIGGER_DEPLOY="${TRIGGER_DEPLOY:-true}"

for var_name in RENDER_API_KEY RENDER_FRONTEND_SERVICE_ID API_BASE_URL; do
  if [[ -z "${!var_name:-}" ]]; then
    echo "${var_name} is required." >&2
    exit 1
  fi
done

AUTH_HEADER="Authorization: Bearer ${RENDER_API_KEY}"

api_call() {
  local method="$1"
  local url="$2"
  local data="${3:-}"
  local body_file
  body_file="$(mktemp)"
  local http_code

  if [[ -n "$data" ]]; then
    http_code="$(curl -sS -o "$body_file" -w "%{http_code}" -X "$method" \
      -H "${AUTH_HEADER}" \
      -H "Content-Type: application/json" \
      -d "$data" \
      "$url")"
  else
    http_code="$(curl -sS -o "$body_file" -w "%{http_code}" -X "$method" \
      -H "${AUTH_HEADER}" \
      "$url")"
  fi

  if [[ "$http_code" -ge 200 && "$http_code" -lt 300 ]]; then
    rm -f "$body_file"
    return 0
  fi

  echo "Render API ${method} ${url} failed with HTTP ${http_code}" >&2
  cat "$body_file" >&2
  rm -f "$body_file"
  return 1
}

payload="$(python3 - <<'PY' "${API_BASE_URL}"
import json, sys
print(json.dumps({"value": sys.argv[1]}))
PY
)"

api_call PUT "${RENDER_API_BASE_URL}/services/${RENDER_FRONTEND_SERVICE_ID}/env-vars/VITE_API_BASE_URL" "${payload}"
echo "Upserted VITE_API_BASE_URL=${API_BASE_URL} on ${RENDER_FRONTEND_SERVICE_ID}"

if [[ "${TRIGGER_DEPLOY}" == "true" ]]; then
  api_call POST "${RENDER_API_BASE_URL}/services/${RENDER_FRONTEND_SERVICE_ID}/deploys" '{"clearCache":"do_not_clear"}'
  echo "Triggered deploy for ${RENDER_FRONTEND_SERVICE_ID}"
fi

echo "Done. Frontend API base URL synced."
