locals {
  donor_func_app_settings = {
    "ApplicationInsights:LogLevel" = var.APPLICATION_INSIGHTS_LOG_LEVEL

    "AzureStorage:ConnectionString" = var.azure_storage.primary_connection_string

    "HlaMetadataDictionary:AzureStorageConnectionString" = var.azure_storage.primary_connection_string,

    "MacDictionary:AzureStorageConnectionString" = var.azure_storage.primary_connection_string
    "MacDictionary:TableName"                    = var.mac_import_table.name,

    "MessagingServiceBus:ConnectionString"                                                          = var.servicebus_namespace_authorization_rules.read-only.primary_connection_string
    "MessagingServiceBus:DonorManagement:Topic"                                                     = var.servicebus_topics.updated-searchable-donors.name
    "MessagingServiceBus:DonorManagement:SubscriptionForDbA"                                        = azurerm_servicebus_subscription.matching_transient_a.name
    "MessagingServiceBus:DonorManagement:SubscriptionForDbB"                                        = azurerm_servicebus_subscription.matching_transient_b.name
    "MessagingServiceBus:DonorManagement:BatchSize"                                                 = var.MESSAGING_BUS_DONOR_BATCH_SIZE
    "MessagingServiceBus:DonorManagement:CronSchedule"                                              = var.MESSAGING_BUS_DONOR_CRON_SCHEDULE
    "MessagingServiceBus:DonorManagement:OngoingDifferentialDonorUpdatesShouldBeFullyTransactional" = var.DONOR_WRITE_TRANSACTIONALITY__DONOR_UPDATES

    "NotificationsServiceBus:ConnectionString"   = var.servicebus_namespace_authorization_rules.write-only.primary_connection_string
    "NotificationsServiceBus:AlertsTopic"        = var.servicebus_topics.alerts.name
    "NotificationsServiceBus:NotificationsTopic" = var.servicebus_topics.notifications.name

    "WEBSITE_MAX_DYNAMIC_APPLICATION_SCALE_OUT" = "1"
    "WEBSITE_RUN_FROM_PACKAGE"                  = var.WEBSITE_RUN_FROM_PACKAGE
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

  site_config {
    application_insights_key = var.application_insights.instrumentation_key
    application_stack {
      dotnet_version = "v6.0"
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

    ftps_state              = "AllAllowed"
    scm_minimum_tls_version = "1.0"
  }

  tags = var.general.common_tags

  app_settings = local.donor_func_app_settings

  connection_string {
    name  = "SqlA"
    type  = "SQLAzure"
    value = local.matching_transient_database_a_connection_string
  }
  connection_string {
    name  = "SqlB"
    type  = "SQLAzure"
    value = local.matching_transient_database_b_connection_string
  }
  connection_string {
    name  = "PersistentSql"
    type  = "SQLAzure"
    value = local.matching_persistent_database_connection_string
  }
  connection_string {
    name  = "DonorSql"
    type  = "SQLAzure"
    value = "Server=tcp:${var.sql_server.fully_qualified_domain_name},1433;Initial Catalog=${var.donor_import_sql_database.name};Persist Security Info=False;User ID=${var.DONOR_IMPORT_DATABASE_USERNAME};Password=${var.DONOR_IMPORT_DATABASE_PASSWORD};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=1800;"
  }
}
