output "api_service_name" {
  value       = google_cloud_run_v2_service.api.name
  description = "Cloud Run API service name."
}

output "api_base_url" {
  value       = google_cloud_run_v2_service.api.uri
  description = "Base URL for the deployed API."
}

output "sql_instance_name" {
  value       = google_sql_database_instance.main.name
  description = "Cloud SQL instance name."
}

output "sql_public_ip" {
  value       = google_sql_database_instance.main.public_ip_address
  description = "Cloud SQL public IP address."
}

output "database_name" {
  value       = google_sql_database.app.name
  description = "Application database name."
}

output "db_user" {
  value       = google_sql_user.app.name
  description = "Database username."
}

output "postgres_connection_string" {
  value       = local.db_connection_string
  description = "POSTGRES_CONNECTION_STRING (public IP, for CI use)."
  sensitive   = true
}
