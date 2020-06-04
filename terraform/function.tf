resource "azurerm_function_app" "atlas_function" {
  name                      = "${local.environment}-ATLAS-FUNCTION"
  resource_group_name       = azurerm_resource_group.atlas_resource_group.name
  location                  = local.location
  app_service_plan_id       = azurerm_app_service_plan.atlas.id
  https_only                = true
  version                   = "~3"
  storage_connection_string = azurerm_storage_account.function_storage.primary_connection_string

  tags = local.common_tags

  app_settings = {
    // APPINSIGHTS_INSTRUMENTATIONKEY
    //      The azure functions dashboard requires the instrumentation key with this name to integrate with application insights.
    "APPINSIGHTS_INSTRUMENTATIONKEY"            = azurerm_application_insights.atlas.instrumentation_key
    "ApplicationInsights:LogLevel"              = var.APPLICATION_INSIGHTS_LOG_LEVEL
    "MessagingServiceBus:ConnectionString"      = azurerm_servicebus_namespace_authorization_rule.read-write.primary_connection_string
    "MessagingServiceBus:SearchRequestsQueue"   = module.matching_algorithm.general.search_requests_queue
    "MessagingServiceBus:SearchResultsTopic"    = module.matching_algorithm.general.search_results_topic
    "WEBSITE_MAX_DYNAMIC_APPLICATION_SCALE_OUT" = "1"
    "WEBSITE_RUN_FROM_PACKAGE"                  = var.WEBSITE_RUN_FROM_PACKAGE
    "MacImport:ConnectionString"                = azurerm_storage_account.azure_storage.primary_connection_string
    "MacImport:TableName"                       = module.multiple_allele_code_lookup.general.storage_table_name
  }
}
