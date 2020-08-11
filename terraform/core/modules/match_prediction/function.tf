locals {
  atlas_match_prediction_function_name = "${var.general.environment}-ATLAS-MATCH-PREDICTION-FUNCTION"
}

resource "azurerm_function_app" "atlas_match_prediction_function" {
  name                      = local.atlas_match_prediction_function_name
  resource_group_name       = var.app_service_plan.resource_group_name
  location                  = var.general.location
  app_service_plan_id       = var.app_service_plan.id
  https_only                = true
  version                   = "~3"
  storage_connection_string = var.function_storage.primary_connection_string

  tags = var.general.common_tags

  app_settings = {
    // APPINSIGHTS_INSTRUMENTATIONKEY
    //      The azure functions dashboard requires the instrumentation key with this name to integrate with application insights.
    "APPINSIGHTS_INSTRUMENTATIONKEY"                     = var.application_insights.instrumentation_key
    "ApplicationInsights:LogLevel"                       = var.APPLICATION_INSIGHTS_LOG_LEVEL
    "AzureStorage:ConnectionString"                      = var.azure_storage.primary_connection_string
    "HlaMetadataDictionary:AzureStorageConnectionString" = var.azure_storage.primary_connection_string
    "MacDictionary:AzureStorageConnectionString"         = var.azure_storage.primary_connection_string
    "MacDictionary:TableName"                            = var.mac_import_table.name,
    "NotificationsServiceBus:ConnectionString"           = var.servicebus_namespace_authorization_rules.write-only.primary_connection_string
    "NotificationsServiceBus:AlertsTopic"                = var.servicebus_topics.alerts.name
    "NotificationsServiceBus:NotificationsTopic"         = var.servicebus_topics.notifications.name
    "WEBSITE_MAX_DYNAMIC_APPLICATION_SCALE_OUT"          = "1"
    "WEBSITE_RUN_FROM_PACKAGE"                           = var.WEBSITE_RUN_FROM_PACKAGE
  }

  site_config {
    ip_restriction = var.IP_RESTRICTION_SETTINGS
  }

  dynamic "ip_restriction" {
    for_each = var.IP_RESTRICTION_SETTINGS
    content {
      ip_address  = ip_restriction.value.ip_address
      subnet_mask = ip_restriction.value.subnet_mask
    }
  }

  connection_string {
    name  = "MatchPredictionSql"
    type  = "SQLAzure"
    value = local.match_prediction_database_connection_string
  }
}
