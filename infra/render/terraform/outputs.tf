output "frontend_service_id" {
  value       = render_static_site.frontend.id
  description = "Render service id for the frontend static site."
}

output "frontend_url" {
  value       = render_static_site.frontend.url
  description = "Public URL for the frontend."
}
