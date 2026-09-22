locals {
  donor_func_app_settings = {
    "ApplicationInsights:LogLevel" = var.APPLICATION_INSIGHTS_LOG_LEVEL

    "AutoMapper:LicenseKey" = var.AUTOMAPPER_LICENSE_KEY

    "AzureStorage:ConnectionString" = var.key_vault.secret_refs["azure-storage-connection-string"]

    "HlaMetadataDictionary:AzureStorageConnectionString" = var.key_vault.secret_refs["azure-storage-connection-string"],

    "MacDictionary:AzureStorageConnectionString" = var.key_vault.secret_refs["azure-storage-connection-string"]
    "MacDictionary:TableName"                    = var.mac_import_table.name,

    "MessagingServiceBus:ConnectionString"                                                          = var.key_vault.secret_refs["servicebus-read-only-connection-string"]
    "MessagingServiceBus:DonorManagement:Topic"                                                     = var.servicebus_topics.updated-searchable-donors.name
    "MessagingServiceBus:DonorManagement:SubscriptionForDbA"                                        = azurerm_servicebus_subscription.matching_transient_a.name
    "MessagingServiceBus:DonorManagement:SubscriptionForDbB"                                        = azurerm_servicebus_subscription.matching_transient_b.name
    "MessagingServiceBus:DonorManagement:BatchSize"                                                 = var.MESSAGING_BUS_DONOR_BATCH_SIZE
    "MessagingServiceBus:DonorManagement:CronSchedule"                                              = var.MESSAGING_BUS_DONOR_CRON_SCHEDULE
    "MessagingServiceBus:DonorManagement:OngoingDifferentialDonorUpdatesShouldBeFullyTransactional" = var.DONOR_WRITE_TRANSACTIONALITY__DONOR_UPDATES
    "MessagingServiceBus:SendRetryCount"                                                            = var.SERVICE_BUS_SEND_RETRY_COUNT
    "MessagingServiceBus:SendRetryCooldownSeconds"                                                  = var.SERVICE_BUS_SEND_RETRY_COOLDOWN_SECONDS

    "NotificationsServiceBus:ConnectionString"         = var.key_vault.secret_refs["servicebus-write-only-connection-string"]
    "NotificationsServiceBus:AlertsTopic"              = var.servicebus_topics.alerts.name
    "NotificationsServiceBus:NotificationsTopic"       = var.servicebus_topics.notifications.name
    "NotificationsServiceBus:SendRetryCount"           = var.SERVICE_BUS_SEND_RETRY_COUNT
    "NotificationsServiceBus:SendRetryCooldownSeconds" = var.SERVICE_BUS_SEND_RETRY_COOLDOWN_SECONDS

    "WEBSITE_RUN_FROM_PACKAGE" = var.WEBSITE_RUN_FROM_PACKAGE
  }
  donor_management_function_app_name = "${var.general.environment}-ATLAS-MATCHING-DONOR-MANAGEMENT-FUNCTION"
}

resource "azurerm_windows_function_app" "atlas_matching_algorithm_donor_management_function" {
  name                        = local.donor_management_function_app_name
  resource_group_name         = var.elastic_app_service_plan.resource_group_name
  location                    = var.general.location
  service_plan_id             = var.elastic_app_service_plan.id
  client_certificate_mode     = "Required"
  https_only                  = true
  functions_extension_version = "~4"
  storage_account_access_key  = var.shared_function_storage.primary_access_key
  storage_account_name        = var.shared_function_storage.name

  // Used to resolve the @Microsoft.KeyVault app settings and connection strings below. The role assignment that lets
  // this identity read the vault lives in the root module, so it is ordered by the depends_on on the module block.
  identity {
    type         = "UserAssigned"
    identity_ids = [var.key_vault.function_apps_identity_id]
  }

  key_vault_reference_identity_id = var.key_vault.function_apps_identity_id

  site_config {
    application_insights_key = var.application_insights.instrumentation_key
    application_stack {
      dotnet_version              = "v8.0"
      use_dotnet_isolated_runtime = true
    }
    cors {
      support_credentials = false
    }
    dynamic "ip_restriction" {
      for_each = var.IP_RESTRICTION_SETTINGS
      content {
        ip_address = ip_restriction
      }
    }

    health_check_path                 = "/api/HealthCheck"
    health_check_eviction_time_in_min = 10

    pre_warmed_instance_count = 1
    app_scale_limit           = 1

    ftps_state              = "AllAllowed"
    scm_minimum_tls_version = "1.2"
  }

  tags = var.general.common_tags

  app_settings = local.donor_func_app_settings

  connection_string {
    name  = "SqlA"
    type  = "SQLAzure"
    value = local.sql_kv_ref["matching-transient-a-sql-connection-string"]
  }
  connection_string {
    name  = "SqlB"
    type  = "SQLAzure"
    value = local.sql_kv_ref["matching-transient-b-sql-connection-string"]
  }
  connection_string {
    name  = "PersistentSql"
    type  = "SQLAzure"
    value = local.sql_kv_ref["matching-persistent-sql-connection-string"]
  }
  connection_string {
    name  = "DonorSql"
    type  = "SQLAzure"
    value = local.sql_kv_ref["matching-donor-sql-connection-string"]
  }

  lifecycle {
    ignore_changes = [
      site_config[0].cors,
      tags["hidden-link: /app-insights-resource-id"],
    ]
  }
}
