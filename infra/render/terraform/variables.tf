variable "github_repo_url" {
  type        = string
  description = "HTTPS clone URL for this repository."
  default     = "https://github.com/ClinRS-AI/mock-health-system"
}

variable "github_branch" {
  type        = string
  description = "Git branch for frontend deployments."
  default     = "main"
}

variable "service_name_prefix" {
  type        = string
  description = "Prefix for Render frontend service names."
  default     = "mock-health-system"
}

variable "api_base_url" {
  type        = string
  description = "Cloud Run base URL used for VITE_API_BASE_URL."
}
