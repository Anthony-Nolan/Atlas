resource "azurerm_function_app" "atlas_match_prediction_function" {
  name                      = "${var.general.environment}-ATLAS-MATCH-PREDICTION-FUNCTION"
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
    "HlaMetadataDictionary:AzureStorageConnectionString" = var.azure_storage.primary_connection_string
    "MacDictionary:AzureStorageConnectionString"         = var.azure_storage.primary_connection_string
    "MacDictionary:MacSourceUrl"                         = var.MAC_SOURCE
    "MacDictionary:TableName"                            = var.mac_import_table.name,
    "NotificationsServiceBus:ConnectionString"           = var.servicebus_namespace_authorization_rules.write-only.primary_connection_string
    "NotificationsServiceBus:AlertsTopic"                = var.servicebus_topics.alerts.name
    "NotificationsServiceBus:NotificationsTopic"         = var.servicebus_topics.notifications.name
    "WEBSITE_MAX_DYNAMIC_APPLICATION_SCALE_OUT"          = "1"
    "WEBSITE_RUN_FROM_PACKAGE"                           = var.WEBSITE_RUN_FROM_PACKAGE
  }

  connection_string {
    name  = "MatchPredictionSql"
    type  = "SQLAzure"
    value = local.match_prediction_database_connection_string
  }
}
