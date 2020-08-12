locals {
  atlas_function_app_name = "${local.environment}-ATLAS-FUNCTIONS"
}

resource "azurerm_function_app" "atlas_function" {
  name                      = local.atlas_function_app_name
  resource_group_name       = azurerm_resource_group.atlas_resource_group.name
  location                  = local.location
  app_service_plan_id       = azurerm_app_service_plan.atlas-elastic-plan.id
  https_only                = true
  version                   = "~3"
  storage_connection_string = azurerm_storage_account.function_storage.primary_connection_string

  tags = local.common_tags

  site_config {
    pre_warmed_instance_count = 2
    ip_restriction = [for ip in var.IP_RESTRICTION_SETTINGS : {
      ip_address = ip.ip_address
    }]
  }

  app_settings = {
    // APPINSIGHTS_INSTRUMENTATIONKEY
    //      The azure functions dashboard requires the instrumentation key with this name to integrate with application insights.
    "APPINSIGHTS_INSTRUMENTATIONKEY" = azurerm_application_insights.atlas.instrumentation_key
    "ApplicationInsights:LogLevel"   = var.APPLICATION_INSIGHTS_LOG_LEVEL

    "AtlasFunction:AzureStorage:ConnectionString"           = azurerm_storage_account.azure_storage.primary_connection_string
    "AtlasFunction:AzureStorage:SearchResultsBlobContainer" = azurerm_storage_container.search_results_blob_container.name
    "AtlasFunction:MessagingServiceBus:ConnectionString"    = azurerm_servicebus_namespace_authorization_rule.write-only.primary_connection_string
    "AtlasFunction:MessagingServiceBus:SearchResultsTopic"  = azurerm_servicebus_topic.search-results-ready.name,
    "AtlasFunction:Orchestration:MatchPredictionBatchSize"  = var.ORCHESTRATION_MATCH_PREDICTION_BATCH_SIZE,

    "HlaMetadataDictionary:AzureStorageConnectionString" = azurerm_storage_account.azure_storage.primary_connection_string
    "HlaMetadataDictionary:HlaNomenclatureSourceUrl"     = var.WMDA_FILE_URL

    "MacDictionary:AzureStorageConnectionString" = azurerm_storage_account.azure_storage.primary_connection_string
    "MacDictionary:Import:CronSchedule"          = var.MAC_IMPORT_CRON_SCHEDULE
    "MacDictionary:Import:MacSourceUrl"          = var.MAC_SOURCE
    "MacDictionary:TableName"                    = module.multiple_allele_code_lookup.storage_table.name

    "Matching:AzureStorage:ConnectionString"           = azurerm_storage_account.azure_storage.primary_connection_string
    "Matching:AzureStorage:SearchResultsBlobContainer" = module.matching_algorithm.azure_storage.search_results_container
    "Matching:MessagingServiceBus:ConnectionString"    = azurerm_servicebus_namespace_authorization_rule.read-write.primary_connection_string
    "Matching:MessagingServiceBus:SearchRequestsQueue" = module.matching_algorithm.service_bus.search_requests_queue
    "Matching:MessagingServiceBus:SearchResultsTopic"  = module.matching_algorithm.service_bus.search_results_topic

    "NotificationsServiceBus:AlertsTopic"        = module.support.general.alerts_servicebus_topic.name
    "NotificationsServiceBus:ConnectionString"   = azurerm_servicebus_namespace_authorization_rule.write-only.primary_connection_string
    "NotificationsServiceBus:NotificationsTopic" = module.support.general.notifications_servicebus_topic.name

    "WEBSITE_RUN_FROM_PACKAGE" = var.WEBSITE_RUN_FROM_PACKAGE
  }

  connection_string {
    name  = "DonorImport:Sql"
    type  = "SQLAzure"
    value = module.donor_import.sql_database.connection_string
  }

  connection_string {
    name  = "Matching:Sql:Persistent"
    type  = "SQLAzure"
    value = module.matching_algorithm.sql_database.persistent_database_connection_string
  }

  connection_string {
    name  = "Matching:Sql:A"
    type  = "SQLAzure"
    value = module.matching_algorithm.sql_database.transient_a_database_connection_string
  }

  connection_string {
    name  = "Matching:Sql:B"
    type  = "SQLAzure"
    value = module.matching_algorithm.sql_database.transient_b_database_connection_string
  }

  connection_string {
    name  = "MatchPrediction:Sql"
    type  = "SQLAzure"
    value = module.match_prediction.sql_database.connection_string
  }
}
