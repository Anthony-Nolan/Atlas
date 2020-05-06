resource "azurerm_function_app" "atlas_match_prediction_function" {
  name                      = "${local.environment}-ATLAS-MATCH-PREDICTION-FUNCTION"
  resource_group_name       = azurerm_resource_group.atlas_resource_group.name
  location                  = local.location
  app_service_plan_id       = azurerm_app_service_plan.atlas.id
  https_only                = true
  version                   = "~2"
  storage_connection_string = azurerm_storage_account.shared_function_storage.primary_connection_string

  tags = local.common_tags

  app_settings = {
    "ApplicationInsights.InstrumentationKey"    = azurerm_application_insights.atlas.instrumentation_key
    //  The azure functions dashboard requires the instrumentation key with this name to integrate with application insights
    "APPINSIGHTS_INSTRUMENTATIONKEY"            = azurerm_application_insights.atlas.instrumentation_key
    "ApplicationInsights.LogLevel"              = var.APPLICATION_INSIGHTS_LOG_LEVEL
    "AzureStorage.ConnectionString"             = azurerm_storage_account.azure_storage.primary_connection_string
    "WEBSITE_MAX_DYNAMIC_APPLICATION_SCALE_OUT" = "1"
    "WEBSITE_RUN_FROM_PACKAGE"                  = var.WEBSITE_RUN_FROM_PACKAGE
  }
}
