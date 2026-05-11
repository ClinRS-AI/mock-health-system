resource "render_static_site" "frontend" {
  name          = var.service_name_prefix
  repo_url      = var.github_repo_url
  branch        = var.github_branch
  build_command = "cd frontend && npm ci && npm run build"
  publish_path  = "frontend/dist"
  auto_deploy   = true

  env_vars = {
    VITE_API_BASE_URL = { value = var.api_base_url }
  }

  routes = [
    {
      type        = "rewrite"
      source      = "/*"
      destination = "/index.html"
    }
  ]
}
