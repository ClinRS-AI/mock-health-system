moved {
  from = google_cloud_run_v2_service_iam_member.invoker
  to   = google_cloud_run_v2_service_iam_member.invoker_no_public_tag
}

data "google_project" "current" {
  project_id = var.project_id
}

locals {
  sql_instance_name         = "${var.name_prefix}-sql"
  api_service_name          = "${var.name_prefix}-api"
  soap_secret_name          = "${var.name_prefix}-soap-report-password"
  admin_secret_name         = "${var.name_prefix}-admin-key"
  has_ci_authorized_network = trimspace(var.ci_authorized_network_cidr) != ""
  effective_authorized_networks = concat(
    var.authorized_networks,
    local.has_ci_authorized_network ? [{
      name  = "github-actions-ci"
      value = trimspace(var.ci_authorized_network_cidr)
    }] : []
  )
  db_connection_string        = "Host=${google_sql_database_instance.main.public_ip_address};Port=5432;Database=${google_sql_database.app.name};Username=${google_sql_user.app.name};Password=${var.app_db_password};SSL Mode=Require;Trust Server Certificate=true;Include Error Detail=true"
  cloud_run_connection_string = "Host=/cloudsql/${google_sql_database_instance.main.connection_name};Port=5432;Database=${google_sql_database.app.name};Username=${google_sql_user.app.name};Password=${var.app_db_password};Include Error Detail=true"
  # When org policy restricts principals (e.g. iam.allowedPolicyMemberDomains), bind a tag value
  # that your conditional org policy exempts, then grant run.invoker to allUsers.
  create_public_invoker_tag_binding = (
    trimspace(var.cloud_run_invoker_member) != "" &&
    trimspace(var.cloud_run_public_invoker_tag_value) != ""
  )
}

resource "google_sql_database_instance" "main" {
  name                = local.sql_instance_name
  region              = var.region
  database_version    = var.database_version
  deletion_protection = var.deletion_protection

  settings {
    tier              = var.tier
    disk_size         = var.disk_size_gb
    disk_type         = var.disk_type
    disk_autoresize   = true
    availability_type = var.availability_type

    backup_configuration {
      enabled                        = true
      point_in_time_recovery_enabled = true
    }

    ip_configuration {
      ipv4_enabled = var.enable_public_ip
      ssl_mode     = "ALLOW_UNENCRYPTED_AND_ENCRYPTED"

      dynamic "authorized_networks" {
        for_each = local.effective_authorized_networks
        content {
          name  = authorized_networks.value.name
          value = authorized_networks.value.value
        }
      }
    }
  }
}

resource "google_sql_database" "app" {
  name     = var.database_name
  instance = google_sql_database_instance.main.name
}

resource "google_sql_user" "app" {
  name            = var.app_db_user
  instance        = google_sql_database_instance.main.name
  password        = var.app_db_password
  deletion_policy = "ABANDON"
}

resource "google_secret_manager_secret" "soap_report_password" {
  secret_id = local.soap_secret_name

  replication {
    auto {}
  }
}

resource "google_secret_manager_secret_version" "soap_report_password" {
  secret      = google_secret_manager_secret.soap_report_password.id
  secret_data = var.soap_report_password
}

resource "google_secret_manager_secret" "admin_key" {
  secret_id = local.admin_secret_name

  replication {
    auto {}
  }
}

resource "google_secret_manager_secret_version" "admin_key" {
  secret      = google_secret_manager_secret.admin_key.id
  secret_data = var.admin_key
}

resource "google_service_account" "api_runtime" {
  account_id   = "${replace(var.name_prefix, "-", "")}api"
  display_name = "Mock Health System API runtime"
}

# Cloud Run uses the Unix socket under /cloudsql; the runtime SA needs roles/cloudsql.client
# (includes cloudsql.instances.get / connect). Many CI deploy principals cannot set project IAM;
# set manage_cloudsql_client_iam=false (default) and grant the binding once with gcloud (see variable description).
resource "google_project_iam_member" "api_runtime_cloudsql_client" {
  count   = var.manage_cloudsql_client_iam ? 1 : 0
  project = var.project_id
  role    = "roles/cloudsql.client"
  member  = "serviceAccount:${google_service_account.api_runtime.email}"
}

resource "google_secret_manager_secret_iam_member" "soap_secret_accessor" {
  secret_id = google_secret_manager_secret.soap_report_password.id
  role      = "roles/secretmanager.secretAccessor"
  member    = "serviceAccount:${google_service_account.api_runtime.email}"
}

resource "google_secret_manager_secret_iam_member" "admin_secret_accessor" {
  secret_id = google_secret_manager_secret.admin_key.id
  role      = "roles/secretmanager.secretAccessor"
  member    = "serviceAccount:${google_service_account.api_runtime.email}"
}

resource "google_cloud_run_v2_service" "api" {
  name     = local.api_service_name
  location = var.region
  ingress  = "INGRESS_TRAFFIC_ALL"

  template {
    service_account = google_service_account.api_runtime.email

    scaling {
      min_instance_count = var.api_min_instances
      max_instance_count = var.api_max_instances
    }

    timeout = "300s"

    containers {
      image = var.api_image

      ports {
        container_port = var.api_port
      }

      resources {
        startup_cpu_boost = true
      }

      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = "Production"
      }

      env {
        name  = "ASPNETCORE_URLS"
        value = "http://0.0.0.0:8080"
      }

      env {
        name  = "POSTGRES_CONNECTION_STRING"
        value = local.cloud_run_connection_string
      }

      env {
        name  = "APPLY_EFMIGRATIONS_ON_STARTUP"
        value = "true"
      }

      env {
        name = "SOAP_REPORT_PASSWORD"
        value_source {
          secret_key_ref {
            secret  = google_secret_manager_secret.soap_report_password.secret_id
            version = "latest"
          }
        }
      }

      env {
        name = "AUTH_SETTINGS_ADMIN_KEY"
        value_source {
          secret_key_ref {
            secret  = google_secret_manager_secret.admin_key.secret_id
            version = "latest"
          }
        }
      }

      dynamic "env" {
        for_each = trimspace(var.frontend_url) != "" ? [trimspace(var.frontend_url)] : []
        content {
          name  = "Cors__AllowedOrigins__0"
          value = env.value
        }
      }

      volume_mounts {
        name       = "cloudsql"
        mount_path = "/cloudsql"
      }
    }

    volumes {
      name = "cloudsql"
      cloud_sql_instance {
        instances = [google_sql_database_instance.main.connection_name]
      }
    }
  }

  depends_on = [
    google_sql_database.app,
    google_sql_user.app,
    google_secret_manager_secret_version.soap_report_password,
    google_secret_manager_secret_version.admin_key,
    google_secret_manager_secret_iam_member.soap_secret_accessor,
    google_secret_manager_secret_iam_member.admin_secret_accessor,
  ]
}

# Binds a Resource Manager tag to the regional Cloud Run service parent so a conditional
# org policy (e.g. on iam.allowedPolicyMemberDomains) can allow allUsers only for tagged services.
# Create the tag key/value and conditional policy in the org/project first; pass tagValues/{id} or
# the namespaced value id here. The deploy principal needs tagBindings permissions on this parent.
resource "google_tags_location_tag_binding" "api_public_invoker" {
  count = local.create_public_invoker_tag_binding ? 1 : 0

  parent    = "//run.googleapis.com/projects/${data.google_project.current.number}/locations/${var.region}/services/${google_cloud_run_v2_service.api.name}"
  tag_value = trimspace(var.cloud_run_public_invoker_tag_value)
  location  = var.region

  depends_on = [google_cloud_run_v2_service.api]
}

# Split so depends_on stays static (Terraform does not allow dynamic concat() here).
resource "google_cloud_run_v2_service_iam_member" "invoker_after_public_tag" {
  count    = var.cloud_run_invoker_member != "" && local.create_public_invoker_tag_binding ? 1 : 0
  name     = google_cloud_run_v2_service.api.name
  location = google_cloud_run_v2_service.api.location
  role     = "roles/run.invoker"
  member   = var.cloud_run_invoker_member

  depends_on = [
    google_cloud_run_v2_service.api,
    google_tags_location_tag_binding.api_public_invoker[0],
  ]
}

resource "google_cloud_run_v2_service_iam_member" "invoker_no_public_tag" {
  count    = var.cloud_run_invoker_member != "" && !local.create_public_invoker_tag_binding ? 1 : 0
  name     = google_cloud_run_v2_service.api.name
  location = google_cloud_run_v2_service.api.location
  role     = "roles/run.invoker"
  member   = var.cloud_run_invoker_member

  depends_on = [google_cloud_run_v2_service.api]
}
