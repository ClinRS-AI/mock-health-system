#!/usr/bin/env bash
set -euo pipefail

# Deploy mock-health-system:
#  1) Apply GCP Terraform (Cloud SQL + Cloud Run API)
#  2) Optionally apply Render Terraform (create frontend static site)
#  3) Sync VITE_API_BASE_URL on Render and trigger a redeploy
#
# Required env vars:
#   TF_VAR_project_id
#   TF_VAR_app_db_password
#   TF_VAR_api_image
#   TF_VAR_soap_report_password
#   TF_VAR_admin_key
#   TF_STATE_BUCKET
#
# Required for Render operations:
#   RENDER_API_KEY
#   RENDER_OWNER_ID (only when CREATE_FRONTEND=true)
#   RENDER_FRONTEND_SERVICE_ID (when CREATE_FRONTEND is not true)
#
# Optional:
#   TF_VAR_region
#   TF_VAR_name_prefix
#   TF_VAR_frontend_url
#   GCP_TERRAFORM_DIR   (default: infra/gcp/terraform)
#   RENDER_TERRAFORM_DIR (default: infra/render/terraform)
#   TF_STATE_PREFIX      (default: mock-health-system/gcp)
#   RENDER_TF_STATE_PREFIX (default: mock-health-system/render)
#   AUTO_APPROVE         (default: true)
#   CREATE_FRONTEND      (default: false)
#   SKIP_FRONTEND_SYNC   (default: false)
#   TRIGGER_DEPLOY       (default: true)

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
GCP_TERRAFORM_DIR="${GCP_TERRAFORM_DIR:-${REPO_ROOT}/infra/gcp/terraform}"
RENDER_TERRAFORM_DIR="${RENDER_TERRAFORM_DIR:-${REPO_ROOT}/infra/render/terraform}"
AUTO_APPROVE="${AUTO_APPROVE:-true}"
CREATE_FRONTEND="${CREATE_FRONTEND:-false}"
SKIP_FRONTEND_SYNC="${SKIP_FRONTEND_SYNC:-false}"
TRIGGER_DEPLOY="${TRIGGER_DEPLOY:-true}"
TF_STATE_PREFIX="${TF_STATE_PREFIX:-mock-health-system/gcp}"
RENDER_TF_STATE_PREFIX="${RENDER_TF_STATE_PREFIX:-mock-health-system/render}"

require_env() {
  local key="$1"
  if [[ -z "${!key:-}" ]]; then
    echo "${key} is required." >&2
    exit 1
  fi
}

require_env TF_VAR_project_id
require_env TF_VAR_app_db_password
require_env TF_VAR_api_image
require_env TF_VAR_soap_report_password
require_env TF_VAR_admin_key
require_env TF_STATE_BUCKET

if [[ "${CREATE_FRONTEND}" == "true" || "${SKIP_FRONTEND_SYNC}" != "true" ]]; then
  require_env RENDER_API_KEY
fi

if [[ "${CREATE_FRONTEND}" == "true" ]]; then
  require_env RENDER_OWNER_ID
fi

if [[ "${SKIP_FRONTEND_SYNC}" != "true" && "${CREATE_FRONTEND}" != "true" ]]; then
  require_env RENDER_FRONTEND_SERVICE_ID
fi

echo "[1/3] Applying GCP Terraform in ${GCP_TERRAFORM_DIR}..."
cd "${GCP_TERRAFORM_DIR}"
terraform init \
  -backend-config="bucket=${TF_STATE_BUCKET}" \
  -backend-config="prefix=${TF_STATE_PREFIX}"

echo "Attempting idempotent imports (ignoring failures)..."
name_prefix="${TF_VAR_name_prefix:-mock-health-system}"
sql_instance_name="${name_prefix}-sql"
sa_account_id="$(python3 - <<'PY'
import os
prefix = os.environ.get("TF_VAR_name_prefix", "mock-health-system")
print(prefix.replace("-", "") + "api")
PY
)"
sa_email="${sa_account_id}@${TF_VAR_project_id}.iam.gserviceaccount.com"

terraform import -no-color google_sql_database_instance.main "projects/${TF_VAR_project_id}/instances/${sql_instance_name}" >/dev/null 2>&1 || true
terraform import -no-color google_sql_database.app "projects/${TF_VAR_project_id}/instances/${sql_instance_name}/databases/mock_health_system_db" >/dev/null 2>&1 || true
terraform import -no-color google_sql_user.app "${TF_VAR_project_id}/${sql_instance_name}/mock_health_user" >/dev/null 2>&1 || true
terraform import -no-color google_secret_manager_secret.soap_report_password "projects/${TF_VAR_project_id}/secrets/${name_prefix}-soap-report-password" >/dev/null 2>&1 || true
terraform import -no-color google_secret_manager_secret.admin_key "projects/${TF_VAR_project_id}/secrets/${name_prefix}-admin-key" >/dev/null 2>&1 || true
terraform import -no-color google_service_account.api_runtime "projects/${TF_VAR_project_id}/serviceAccounts/${sa_email}" >/dev/null 2>&1 || true

if [[ "${AUTO_APPROVE}" == "true" ]]; then
  terraform apply -auto-approve
else
  terraform apply
fi

API_BASE_URL="$(terraform output -raw api_base_url)"
if [[ -z "${API_BASE_URL}" ]]; then
  echo "Failed to resolve api_base_url from Terraform outputs." >&2
  exit 1
fi
echo "API URL: ${API_BASE_URL}"

if [[ "${CREATE_FRONTEND}" == "true" ]]; then
  echo "[2/3] Applying Render Terraform in ${RENDER_TERRAFORM_DIR}..."
  cd "${RENDER_TERRAFORM_DIR}"
  terraform init \
    -backend-config="bucket=${TF_STATE_BUCKET}" \
    -backend-config="prefix=${RENDER_TF_STATE_PREFIX}"

  export TF_VAR_api_base_url="${API_BASE_URL}"

  if [[ "${AUTO_APPROVE}" == "true" ]]; then
    terraform apply -auto-approve
  else
    terraform apply
  fi

  RENDER_FRONTEND_SERVICE_ID="$(terraform output -raw frontend_service_id)"
  echo "Frontend URL: $(terraform output -raw frontend_url)"
  export RENDER_FRONTEND_SERVICE_ID
fi

if [[ "${SKIP_FRONTEND_SYNC}" == "true" ]]; then
  echo "[3/3] Skipping Render frontend sync."
  echo "Done."
  exit 0
fi

echo "[3/3] Syncing VITE_API_BASE_URL on Render..."
cd "${REPO_ROOT}/infra/render/terraform"
RENDER_API_KEY="${RENDER_API_KEY}" \
RENDER_FRONTEND_SERVICE_ID="${RENDER_FRONTEND_SERVICE_ID}" \
API_BASE_URL="${API_BASE_URL}" \
TRIGGER_DEPLOY="${TRIGGER_DEPLOY}" \
./scripts/sync-frontend-api-base-url.sh

echo "Done."
