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
  description = "X-Admin-Key value for admin endpoints (AUTH_SETTINGS_ADMIN_KEY)."
}

variable "frontend_url" {
  type        = string
  description = "Frontend URL allowed by CORS (Render static site URL)."
  default     = ""
}

variable "cloud_run_invoker_member" {
  type        = string
  description = "IAM member granted roles/run.invoker on the Cloud Run service."
  default     = "allUsers"
}
