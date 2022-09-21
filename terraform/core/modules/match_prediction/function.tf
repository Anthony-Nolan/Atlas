locals {
  atlas_match_prediction_function_name = "${var.general.environment}-ATLAS-MATCH-PREDICTION-FUNCTION"
}

resource "azurerm_function_app" "atlas_match_prediction_function" {
  name                       = local.atlas_match_prediction_function_name
  resource_group_name        = var.app_service_plan.resource_group_name
  location                   = var.general.location
  app_service_plan_id        = var.app_service_plan.id
  https_only                 = true
  version                    = "~4"
  storage_account_access_key = var.shared_function_storage.primary_access_key
  storage_account_name       = var.shared_function_storage.name

  tags = var.general.common_tags

  app_settings = {
    // APPINSIGHTS_INSTRUMENTATIONKEY
    //      The azure functions dashboard requires the instrumentation key with this name to integrate with application insights.
    "APPINSIGHTS_INSTRUMENTATIONKEY" = var.application_insights.instrumentation_key
    "ApplicationInsights:LogLevel"   = var.APPLICATION_INSIGHTS_LOG_LEVEL

    "AzureStorage:ConnectionString"                    = var.azure_storage.primary_connection_string
    "AzureStorage:MatchPredictionResultsBlobContainer" = azurerm_storage_container.match_prediction_results_container.name

    "FUNCTIONS_WORKER_RUNTIME" : "dotnet"

    "HlaMetadataDictionary:AzureStorageConnectionString" = var.azure_storage.primary_connection_string

    "MacDictionary:AzureStorageConnectionString" = var.azure_storage.primary_connection_string
    "MacDictionary:TableName"                    = var.mac_import_table.name,

    "MessagingServiceBus:ConnectionString"                                     = var.servicebus_namespace_authorization_rules.read-write.primary_connection_string
    "MessagingServiceBus:ImportFileSubscription"                               = azurerm_servicebus_subscription.haplotype-frequency-file-processor.name
    "MessagingServiceBus:ImportFileTopic"                                      = azurerm_servicebus_topic.haplotype-frequency-file-uploads.name
    "MatchPredictionAlgorithm:SuppressCompressedPhenotypeConversionExceptions" = var.SUPPRESS_COMPRESSED_PHENOTYPE_CONVERSION_EXCEPTIONS
    "MatchPredictionRequests:ServiceBusSubscription"                           = azurerm_servicebus_subscription.match-prediction-request-runner.name
    "MatchPredictionRequests:ServiceBusTopic"                                  = azurerm_servicebus_topic.match-prediction-requests.name

    "NotificationsServiceBus:ConnectionString"   = var.servicebus_namespace_authorization_rules.write-only.primary_connection_string
    "NotificationsServiceBus:AlertsTopic"        = var.servicebus_topics.alerts.name
    "NotificationsServiceBus:NotificationsTopic" = var.servicebus_topics.notifications.name

    "WEBSITE_MAX_DYNAMIC_APPLICATION_SCALE_OUT" = "1"
    "WEBSITE_RUN_FROM_PACKAGE"                  = var.WEBSITE_RUN_FROM_PACKAGE
  }

  site_config {
    ip_restriction = [for ip in var.IP_RESTRICTION_SETTINGS : {
      ip_address = ip
      subnet_id  = null
    }]
  }

  connection_string {
    name  = "MatchPredictionSql"
    type  = "SQLAzure"
    value = local.match_prediction_database_connection_string
  }
}
