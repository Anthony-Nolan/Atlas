locals {
  donor_import_function_name = "${var.general.environment}-ATLAS-DONOR-IMPORT-FUNCTION"
}

resource "azurerm_function_app" "atlas_donor_import_function" {
  name                      = local.donor_import_function_name
  resource_group_name       = var.app_service_plan.resource_group_name
  location                  = var.general.location
  app_service_plan_id       = var.app_service_plan.id
  https_only                = true
  version                   = "~3"
  storage_connection_string = var.function_storage.primary_connection_string

  tags = var.general.common_tags

  app_settings = {
    "APPINSIGHTS_INSTRUMENTATIONKEY"               = var.application_insights.instrumentation_key
    "ApplicationInsights:LogLevel"                 = var.APPLICATION_INSIGHTS_LOG_LEVEL
    "AzureStorage:ConnectionString"                = var.azure_storage.primary_connection_string,
    "AzureStorage:DonorFileBlobContainer"          = azurerm_storage_container.donor_blob_storage.name
    "MessagingServiceBus:ConnectionString"         = var.servicebus_namespace_authorization_rules.write-only.primary_connection_string
    "MessagingServiceBus:MatchingDonorUpdateTopic" = azurerm_servicebus_topic.updated-searchable-donors.name
    "NotificationsServiceBus:AlertsTopic"          = var.servicebus_topics.alerts.name
    "NotificationsServiceBus:ConnectionString"     = var.servicebus_namespace_authorization_rules.write-only.primary_connection_string
    "NotificationsServiceBus:NotificationsTopic"   = var.servicebus_topics.notifications.name
    "WEBSITE_RUN_FROM_PACKAGE"                     = "1"
  }

  site_config {
    dynamic "ip_restriction" {
      for_each = var.IP_RESTRICTION_SETTINGS
      content {
        ip_address  = ip_restriction.value.ip_address
        subnet_mask = ip_restriction.value.subnet_mask
      }
    }
  }

  connection_string {
    name  = "DonorStoreSql"
    type  = "SQLAzure"
    value = local.donor_import_connection_string
  }
}
