variable "project_id" {
  type        = string
  description = "GCP project id hosting resources."
}

variable "region" {
  type        = string
  description = "GCP region for resources."
  default     = "us-central1"
}

variable "name_prefix" {
  type        = string
  description = "Prefix used to name resources."
  default     = "mock-health-system"
}

variable "deletion_protection" {
  type        = bool
  description = "Protect Cloud SQL instance from accidental deletion."
  default     = true
}

variable "database_version" {
  type        = string
  description = "Cloud SQL PostgreSQL engine version."
  default     = "POSTGRES_16"
}

variable "tier" {
  type        = string
  description = "Cloud SQL machine tier."
  default     = "db-f1-micro"
}

variable "disk_size_gb" {
  type        = number
  description = "Cloud SQL disk size in GB."
  default     = 20
}

variable "disk_type" {
  type        = string
  description = "Cloud SQL disk type."
  default     = "PD_SSD"
}

variable "availability_type" {
  type        = string
  description = "Cloud SQL availability type."
  default     = "ZONAL"
}

variable "enable_public_ip" {
  type        = bool
  description = "Enable public IP on Cloud SQL."
  default     = true
}

variable "authorized_networks" {
  type = list(object({
    name  = string
    value = string
  }))
  description = "Optional CIDR allowlist for Cloud SQL public access."
  default     = []
}

variable "ci_authorized_network_cidr" {
  type        = string
  description = "Optional CIDR allowlist entry for CI access to Cloud SQL over public IP."
  default     = ""
}

variable "database_name" {
  type        = string
  description = "Application database name."
  default     = "mock_health_system_db"
}

variable "app_db_user" {
  type        = string
  description = "Application database user."
  default     = "mock_health_user"
}

variable "app_db_password" {
  type        = string
  sensitive   = true
  description = "Application database user password."
}

variable "api_image" {
  type        = string
  description = "Container image URI to deploy to Cloud Run."
}

variable "api_port" {
  type        = number
  description = "Container port for the API service."
  default     = 8080
}

variable "api_min_instances" {
  type        = number
  description = "Minimum number of Cloud Run instances."
  default     = 0
}

variable "api_max_instances" {
  type        = number
  description = "Maximum number of Cloud Run instances."
  default     = 2
}

variable "soap_report_password" {
  type        = string
  sensitive   = true
  description = "Shared password for the SOAP report endpoint."
}

variable "admin_key" {
  type        = string
  sensitive   = true
  description = "Static admin secret for AUTH_SETTINGS_ADMIN_KEY. Clients may send it as X-Admin-Key or exchange it for a short-lived JWT (X-Admin-Session) via POST /api/v1/admin/sessions. Optional separate JWT signing secret: set ADMIN_SESSION_SIGNING_KEY or AdminSession__SigningKey on the service if not using Terraform env blocks."
}

variable "frontend_url" {
  type        = string
  description = "Frontend URL allowed by CORS (Render static site URL)."
  default     = ""
}

variable "enable_swagger" {
  type        = bool
  description = "When true, sets ENABLE_SWAGGER on the Cloud Run API so /swagger is available in Production."
  default     = false
}

variable "cloud_run_invoker_member" {
  type        = string
  description = "IAM member granted roles/run.invoker on the Cloud Run service."
  default     = "allUsers"
}

variable "cloud_run_public_invoker_tag_value" {
  type        = string
  description = <<-EOT
    Optional Resource Manager tag value to bind to the Cloud Run service before granting
    cloud_run_invoker_member (e.g. allUsers). Use when organization policy iam.allowedPolicyMemberDomains
    blocks public principals unless the service is tagged: set to tagValues/{numeric_id} or the
    namespaced tag value string from gcloud resource-manager tags values list. Leave empty if
    public invoker is allowed without a tag. Requires a matching conditional org policy that
    exempts resources with this tag.
  EOT
  default     = ""
}

variable "manage_cloudsql_client_iam" {
  type        = bool
  description = <<-EOT
    When true, Terraform grants roles/cloudsql.client to the API runtime service account at the project level.
    Requires the Terraform identity to have permission to modify project IAM (e.g. resourcemanager.projects.setIamPolicy).
    GitHub Actions deploy SAs often cannot; leave false and grant the role once with gcloud as a project owner:
    gcloud projects add-iam-policy-binding PROJECT_ID \
      --member="serviceAccount:SERVICE_ACCOUNT_EMAIL" \
      --role="roles/cloudsql.client"
    Use the Cloud Run runtime service account email (mockhealthsystemapi@PROJECT.iam.gserviceaccount.com for default name_prefix).
  EOT
  default     = false
}
