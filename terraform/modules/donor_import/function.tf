resource "azurerm_function_app" "atlas_donor_import_function" {
  name                      = "${var.general.environment}-ATLAS-DONOR-IMPORT-FUNCTION"
  resource_group_name       = var.app_service_plan.resource_group_name
  location                  = var.general.location
  app_service_plan_id       = var.app_service_plan.id
  https_only                = true
  version                   = "~3"
  storage_connection_string = var.function_storage.primary_connection_string

  tags = var.general.common_tags

  app_settings = {
    "ApplicationInsights:InstrumentationKey"       = var.application_insights.instrumentation_key
    "APPINSIGHTS_INSTRUMENTATIONKEY"               = var.application_insights.instrumentation_key
    "ApplicationInsights:LogLevel"                 = var.APPLICATION_INSIGHTS_LOG_LEVEL
    "AzureStorage:ConnectionString"                = var.azure_storage.primary_connection_string,
    "AzureStorage:DonorFileBlobContainer"          = azurerm_storage_container.donor_blob_storage.name,
    "AzureStorage:DonorFileBlobContainer"          = azurerm_storage_container.donor_blob_storage.name,
    "MessagingServiceBus:ConnectionString"         = var.servicebus_namespace_authorization_rules.write-only.primary_connection_string
    "MessagingServiceBus:MatchingDonorUpdateTopic" = azurerm_servicebus_topic.updated-searchable-donors.name
    "WEBSITE_RUN_FROM_PACKAGE"                     = "1"
  }

  connection_string {
    name  = "Sql"
    type  = "SQLAzure"
    value = local.donor_import_connection_string
  }
}
