locals {
  atlas_function_app_name            = "${local.environment}-ATLAS-FUNCTIONS"
  atlas_public_api_function_app_name = "${local.environment}-ATLAS-API"
}

resource "azurerm_windows_function_app" "atlas_function" {
  name                        = local.atlas_function_app_name
  resource_group_name         = azurerm_resource_group.atlas_resource_group.name
  location                    = local.location
  service_plan_id             = azurerm_service_plan.atlas-elastic-plan.id
  client_certificate_mode     = "Required"
  https_only                  = true
  functions_extension_version = "~4"
  storage_account_access_key  = azurerm_storage_account.atlas_durable_function_storage.primary_access_key
  storage_account_name        = azurerm_storage_account.atlas_durable_function_storage.name

  tags = local.common_tags

  site_config {
    application_insights_key  = azurerm_application_insights.atlas.instrumentation_key
    pre_warmed_instance_count = 1
    use_32_bit_worker         = false
    ftps_state                = "AllAllowed"
    scm_minimum_tls_version   = "1.2"
    cors {
      support_credentials = false
    }
    application_stack {
      dotnet_version = "v6.0"
    }
    dynamic "ip_restriction" {
      for_each = var.IP_RESTRICTION_SETTINGS
      content {
        ip_address = ip_restriction
      }
    }
  }

  app_settings = {
    "ApplicationInsights:LogLevel" = var.APPLICATION_INSIGHTS_LOG_LEVEL

    "AzureFunctionsJobHost__extensions__durableTask__maxConcurrentActivityFunctions" = var.MAX_CONCURRENT_ACTIVITY_FUNCTIONS

    "AtlasFunction:AzureStorage:MatchingConnectionString"                 = azurerm_storage_account.azure_storage.primary_connection_string
    "AtlasFunction:AzureStorage:MatchingResultsBlobContainer"             = module.matching_algorithm.azure_storage.search_results_container
    "AtlasFunction:AzureStorage:RepeatSearchMatchingResultsBlobContainer" = module.repeat_search.storage.repeat_search_matching_results_container_name
    "AtlasFunction:AzureStorage:RepeatSearchResultsBlobContainer"         = module.repeat_search.storage.repeat_search_results_container_name
    "AtlasFunction:AzureStorage:SearchResultsBlobContainer"               = azurerm_storage_container.search_results_blob_container.name
    "AtlasFunction:AzureStorage:MatchPredictionConnectionString"          = azurerm_storage_account.azure_storage.primary_connection_string
    "AtlasFunction:AzureStorage:MatchPredictionRequestsBlobContainer"     = module.match_prediction.storage.match_prediction_requests_container_name
    "AtlasFunction:AzureStorage:MatchPredictionResultsBlobContainer"      = module.match_prediction.storage.match_prediction_results_container_name
    "AtlasFunction:AzureStorage:MatchPredictionDownloadBatchSize"         = var.MATCH_PREDICTION_DOWNLOAD_BATCH_SIZE
    "AtlasFunction:AzureStorage:MatchPredictionProcessingBatchSize"       = var.MATCHING_PREDICTION_PROCESSING_BATCH_SIZE
    "AtlasFunction:AzureStorage:ShouldBatchResults"                       = var.SHOULD_BATCH_RESULTS

    "AtlasFunction:MessagingServiceBus:ConnectionString"                        = azurerm_servicebus_namespace_authorization_rule.read-write.primary_connection_string
    "AtlasFunction:MessagingServiceBus:MatchingResultsSubscription"             = azurerm_servicebus_subscription.match-prediction-orchestration-search-results-ready.name
    "AtlasFunction:MessagingServiceBus:MatchingResultsTopic"                    = module.matching_algorithm.service_bus.matching_results_topic.name
    "AtlasFunction:MessagingServiceBus:RepeatSearchMatchingResultsSubscription" = module.repeat_search.service_bus.repeat_search_matching_results_subscription.name
    "AtlasFunction:MessagingServiceBus:RepeatSearchMatchingResultsTopic"        = module.repeat_search.service_bus.repeat_search_matching_results_topic.name
    "AtlasFunction:MessagingServiceBus:RepeatSearchResultsTopic"                = module.repeat_search.service_bus.repeat_search_results_topic.name
    "AtlasFunction:MessagingServiceBus:SearchResultsTopic"                      = azurerm_servicebus_topic.search-results-ready.name
    "AtlasFunction:MessagingServiceBus:SearchResultsDebugSubscription"          = azurerm_servicebus_subscription.debug-search-results-ready.name
    "AtlasFunction:MessagingServiceBus:RepeatSearchResultsDebugSubscription"    = module.repeat_search.service_bus.repeat_search_results_debug_subscription.name
    "AtlasFunction:Orchestration:MatchPredictionBatchSize"                      = var.ORCHESTRATION_MATCH_PREDICTION_BATCH_SIZE

    "HlaMetadataDictionary:AzureStorageConnectionString"                          = azurerm_storage_account.azure_storage.primary_connection_string
    "HlaMetadataDictionary:HlaNomenclatureSourceUrl"                              = var.WMDA_FILE_URL
    "HlaMetadataDictionary:SearchRelatedMetadata:CacheSlidingExpirationInSeconds" = var.ATLAS_FUNCTION_SEARCH_RELATED_HLA_METADATA_CACHE_SLIDING_EXPIRATION_SEC

    "MacDictionary:AzureStorageConnectionString" = azurerm_storage_account.azure_storage.primary_connection_string
    "MacDictionary:Import:CronSchedule"          = var.MAC_IMPORT_CRON_SCHEDULE
    "MacDictionary:Download:MacSourceUrl"        = var.MAC_SOURCE
    "MacDictionary:TableName"                    = module.multiple_allele_code_lookup.storage_table.name

    "Matching:AzureStorage:ConnectionString"           = azurerm_storage_account.azure_storage.primary_connection_string
    "Matching:AzureStorage:SearchResultsBlobContainer" = module.matching_algorithm.azure_storage.search_results_container
    "Matching:MessagingServiceBus:ConnectionString"    = azurerm_servicebus_namespace_authorization_rule.read-write.primary_connection_string
    "Matching:MessagingServiceBus:SearchRequestsTopic" = module.matching_algorithm.service_bus.matching_requests_topic.name
    "Matching:MessagingServiceBus:SearchResultsTopic"  = module.matching_algorithm.service_bus.matching_results_topic.name

    "MatchPrediction:AzureStorage:ConnectionString"                    = azurerm_storage_account.azure_storage.primary_connection_string
    "MatchPrediction:AzureStorage:MatchPredictionResultsBlobContainer" = module.match_prediction.storage.match_prediction_results_container_name

    "NotificationsServiceBus:AlertsTopic"                     = module.support.general.alerts_servicebus_topic.name
    "NotificationsServiceBus:ConnectionString"                = azurerm_servicebus_namespace_authorization_rule.read-write.primary_connection_string
    "NotificationsServiceBus:Debug:AlertsSubscription"        = module.support.general.alerts_servicebus_debug_subscription
    "NotificationsServiceBus:Debug:NotificationsSubscription" = module.support.general.notifications_servicebus_debug_subscription
    "NotificationsServiceBus:NotificationsTopic"              = module.support.general.notifications_servicebus_topic.name
    "WEBSITE_RUN_FROM_PACKAGE"                                = var.WEBSITE_RUN_FROM_PACKAGE
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

resource "azurerm_windows_function_app" "atlas_public_api_function" {
  name                        = local.atlas_public_api_function_app_name
  resource_group_name         = azurerm_resource_group.atlas_resource_group.name
  location                    = local.location
  service_plan_id             = var.ELASTIC_SERVICE_PLAN_FOR_PUBLIC_API ? azurerm_service_plan.atlas-public-api-elastic-plan[0].id : azurerm_service_plan.atlas-elastic-plan.id
  client_certificate_mode     = "Required"
  https_only                  = true
  functions_extension_version = "~4"
  storage_account_access_key  = azurerm_storage_account.function_storage.primary_access_key
  storage_account_name        = azurerm_storage_account.function_storage.name

  tags = local.common_tags

  site_config {
    application_insights_key  = azurerm_application_insights.atlas.instrumentation_key
    pre_warmed_instance_count = 1
    ftps_state                = "AllAllowed"
    scm_minimum_tls_version   = "1.2"
    cors {
      support_credentials = false
    }
    application_stack {
      dotnet_version = "v6.0"
    }
    dynamic "ip_restriction" {
      for_each = var.IP_RESTRICTION_SETTINGS
      content {
        ip_address = ip_restriction
      }
    }

  }

  app_settings = {
    "ApplicationInsights:LogLevel" = var.APPLICATION_INSIGHTS_LOG_LEVEL

    "Matching:MessagingServiceBus:ConnectionString"    = azurerm_servicebus_namespace_authorization_rule.read-write.primary_connection_string
    "Matching:MessagingServiceBus:SearchRequestsTopic" = module.matching_algorithm.service_bus.matching_requests_topic.name
    "Matching:MessagingServiceBus:SearchResultsTopic"  = module.matching_algorithm.service_bus.matching_results_topic.name

    "RepeatSearch:MessagingServiceBus:ConnectionString"          = azurerm_servicebus_namespace_authorization_rule.read-write.primary_connection_string
    "RepeatSearch:MessagingServiceBus:RepeatSearchRequestsTopic" = module.repeat_search.service_bus.repeat_search_requests_topic.name

    "NotificationsServiceBus:AlertsTopic"        = module.support.general.alerts_servicebus_topic.name
    "NotificationsServiceBus:ConnectionString"   = azurerm_servicebus_namespace_authorization_rule.write-only.primary_connection_string
    "NotificationsServiceBus:NotificationsTopic" = module.support.general.notifications_servicebus_topic.name

    "WEBSITE_RUN_FROM_PACKAGE" = var.WEBSITE_RUN_FROM_PACKAGE
  }

  lifecycle {
    replace_triggered_by = [
      # Recreate the entire functions app if the service plan changes.
      # This is necessary in case of updating var from `true` to `false`, as terraform attempts to destroy the public API service plan /before/ modifying the function app's service_plan_id.
      # This causes a 409 conflict error as the old service plan is still in use by the function app.
      # Adding a `depends_on` to the service plan does not work, so the only option is to recreate the app.
      # WARNING: recreating the app will cause the app host keys to be regenerated.
      terraform_data.elastic_service_plan_for_public_api
    ]
  }
}

resource "terraform_data" "elastic_service_plan_for_public_api" {
  input = var.ELASTIC_SERVICE_PLAN_FOR_PUBLIC_API
}

data "azurerm_function_app_host_keys" "atlas_public_api_function_keys" {
  name                = azurerm_windows_function_app.atlas_public_api_function.name
  resource_group_name = azurerm_resource_group.atlas_resource_group.name
}
