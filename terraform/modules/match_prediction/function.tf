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
    "ApplicationInsights:InstrumentationKey"              = var.application_insights.instrumentation_key
    "APPINSIGHTS_INSTRUMENTATIONKEY"                      = var.application_insights.instrumentation_key
    "ApplicationInsights:LogLevel"                        = var.APPLICATION_INSIGHTS_LOG_LEVEL
    "AzureStorage:ConnectionString"                       = var.azure_storage.primary_connection_string
    "AzureStorage:HaplotypeFrequencySetBlobBlobContainer" = azurerm_storage_container.haplotype_frequency_set_blob_container.name
    "WEBSITE_MAX_DYNAMIC_APPLICATION_SCALE_OUT"           = "1"
    "WEBSITE_RUN_FROM_PACKAGE"                            = var.WEBSITE_RUN_FROM_PACKAGE
  }
}
