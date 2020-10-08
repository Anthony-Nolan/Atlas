locals {
  atlas_function_app_name            = "${local.environment}-ATLAS-FUNCTIONS"
  atlas_public_api_function_app_name = "${local.environment}-ATLAS-API"
}

resource "azurerm_function_app" "atlas_function" {
  name                       = local.atlas_function_app_name
  resource_group_name        = azurerm_resource_group.atlas_resource_group.name
  location                   = local.location
  app_service_plan_id        = azurerm_app_service_plan.atlas-elastic-plan.id
  https_only                 = true
  version                    = "~3"
  storage_account_access_key = azurerm_storage_account.atlas_durable_function_storage.primary_access_key
  storage_account_name       = azurerm_storage_account.atlas_durable_function_storage.name

  tags = local.common_tags

  site_config {
    pre_warmed_instance_count = 1
    ip_restriction = [for ip in var.IP_RESTRICTION_SETTINGS : {
      ip_address = ip
      subnet_id  = null
    }]
  }

  app_settings = {
    // APPINSIGHTS_INSTRUMENTATIONKEY
    //      The azure functions dashboard requires the instrumentation key with this name to integrate with application insights.
    "APPINSIGHTS_INSTRUMENTATIONKEY" = azurerm_application_insights.atlas.instrumentation_key
    "ApplicationInsights:LogLevel"   = var.APPLICATION_INSIGHTS_LOG_LEVEL

    "AtlasFunction:AzureStorage:MatchingConnectionString"             = azurerm_storage_account.azure_storage.primary_connection_string
    "AtlasFunction:AzureStorage:MatchingResultsBlobContainer"         = module.matching_algorithm.azure_storage.search_results_container
    "AtlasFunction:AzureStorage:SearchResultsBlobContainer"           = azurerm_storage_container.search_results_blob_container.name
    "AtlasFunction:AzureStorage:MatchPredictionConnectionString"      = azurerm_storage_account.azure_storage.primary_connection_string
    "AtlasFunction:AzureStorage:MatchPredictionRequestsBlobContainer" = module.match_prediction.storage.match_prediction_requests_container_name
    "AtlasFunction:AzureStorage:MatchPredictionResultsBlobContainer"  = module.match_prediction.storage.match_prediction_results_container_name

    "AtlasFunction:MessagingServiceBus:ConnectionString"            = azurerm_servicebus_namespace_authorization_rule.read-write.primary_connection_string
    "AtlasFunction:MessagingServiceBus:MatchingResultsSubscription" = azurerm_servicebus_subscription.match-prediction-orchestration-search-results-ready.name
    "AtlasFunction:MessagingServiceBus:MatchingResultsTopic"        = module.matching_algorithm.service_bus.matching_results_topic
    "AtlasFunction:MessagingServiceBus:SearchResultsTopic"          = azurerm_servicebus_topic.search-results-ready.name
    "AtlasFunction:Orchestration:MatchPredictionBatchSize"          = var.ORCHESTRATION_MATCH_PREDICTION_BATCH_SIZE

    "FUNCTIONS_WORKER_RUNTIME" : "dotnet"

    "HlaMetadataDictionary:AzureStorageConnectionString" = azurerm_storage_account.azure_storage.primary_connection_string
    "HlaMetadataDictionary:HlaNomenclatureSourceUrl"     = var.WMDA_FILE_URL

    "MacDictionary:AzureStorageConnectionString" = azurerm_storage_account.azure_storage.primary_connection_string
    "MacDictionary:Import:CronSchedule"          = var.MAC_IMPORT_CRON_SCHEDULE
    "MacDictionary:Download:MacSourceUrl"        = var.MAC_SOURCE
    "MacDictionary:TableName"                    = module.multiple_allele_code_lookup.storage_table.name

    "Matching:AzureStorage:ConnectionString"           = azurerm_storage_account.azure_storage.primary_connection_string
    "Matching:AzureStorage:SearchResultsBlobContainer" = module.matching_algorithm.azure_storage.search_results_container
    "Matching:MessagingServiceBus:ConnectionString"    = azurerm_servicebus_namespace_authorization_rule.read-write.primary_connection_string
    "Matching:MessagingServiceBus:SearchRequestsTopic" = module.matching_algorithm.service_bus.matching_requests_topic
    "Matching:MessagingServiceBus:SearchResultsTopic"  = module.matching_algorithm.service_bus.matching_results_topic

    "MatchPrediction:AzureStorage:ConnectionString"                    = azurerm_storage_account.azure_storage.primary_connection_string
    "MatchPrediction:AzureStorage:MatchPredictionResultsBlobContainer" = module.match_prediction.storage.match_prediction_results_container_name

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

resource "azurerm_function_app" "atlas_public_api_function" {
  name                       = local.atlas_public_api_function_app_name
  resource_group_name        = azurerm_resource_group.atlas_resource_group.name
  location                   = local.location
  app_service_plan_id        = azurerm_app_service_plan.atlas-public-api-elastic-plan.id
  https_only                 = true
  version                    = "~3"
  storage_account_access_key = azurerm_storage_account.function_storage.primary_access_key
  storage_account_name       = azurerm_storage_account.function_storage.name

  tags = local.common_tags

  site_config {
    pre_warmed_instance_count = 1
    ip_restriction = [for ip in var.IP_RESTRICTION_SETTINGS : {
      ip_address = ip
      subnet_id  = null
    }]
  }

  app_settings = {
    // APPINSIGHTS_INSTRUMENTATIONKEY
    //      The azure functions dashboard requires the instrumentation key with this name to integrate with application insights.
    "APPINSIGHTS_INSTRUMENTATIONKEY" = azurerm_application_insights.atlas.instrumentation_key
    "ApplicationInsights:LogLevel"   = var.APPLICATION_INSIGHTS_LOG_LEVEL

    "FUNCTIONS_WORKER_RUNTIME" : "dotnet"

    "Matching:MessagingServiceBus:ConnectionString"    = azurerm_servicebus_namespace_authorization_rule.read-write.primary_connection_string
    "Matching:MessagingServiceBus:SearchRequestsTopic" = module.matching_algorithm.service_bus.matching_requests_topic
    "Matching:MessagingServiceBus:SearchResultsTopic"  = module.matching_algorithm.service_bus.matching_results_topic

    "NotificationsServiceBus:AlertsTopic"        = module.support.general.alerts_servicebus_topic.name
    "NotificationsServiceBus:ConnectionString"   = azurerm_servicebus_namespace_authorization_rule.write-only.primary_connection_string
    "NotificationsServiceBus:NotificationsTopic" = module.support.general.notifications_servicebus_topic.name

    "WEBSITE_RUN_FROM_PACKAGE" = var.WEBSITE_RUN_FROM_PACKAGE
  }
}
