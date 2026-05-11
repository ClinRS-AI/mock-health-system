terraform {
  required_version = ">= 1.5"

  required_providers {
    render = {
      source  = "render-oss/render"
      version = "~> 1.8"
    }
  }

  backend "gcs" {}
}

# Credentials are supplied via env vars:
# - RENDER_API_KEY
# - RENDER_OWNER_ID
provider "render" {}
