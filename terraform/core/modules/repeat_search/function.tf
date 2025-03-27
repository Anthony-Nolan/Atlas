locals {
  atlas_repeat_search_function_name = "${var.general.environment}-ATLAS-REPEAT-SEARCH-FUNCTION"
}

resource "azurerm_windows_function_app" "atlas_repeat_search_function" {
  name                        = local.atlas_repeat_search_function_name
  resource_group_name         = var.app_service_plan.resource_group_name
  location                    = var.general.location
  service_plan_id             = var.app_service_plan.id
  client_certificate_mode     = "Required"
  https_only                  = true
  functions_extension_version = "~4"
  storage_account_access_key  = var.shared_function_storage.primary_access_key
  storage_account_name        = var.shared_function_storage.name

  tags = var.general.common_tags

  app_settings = {
    "ApplicationInsights:LogLevel" = var.APPLICATION_INSIGHTS_LOG_LEVEL

    "AzureFunctionsJobHost__extensions__serviceBus__messageHandlerOptions__maxConcurrentCalls" = var.MAX_CONCURRENT_SERVICEBUS_FUNCTIONS

    "AzureAppConfiguration:ConnectionString" = var.azure_app_configuration.primary_read_key[0].connection_string

    "AzureStorage:ConnectionString"             = var.azure_storage.primary_connection_string
    "AzureStorage:MatchingResultsBlobContainer" = azurerm_storage_container.repeat_search_matching_results_container.name
    "AzureStorage:SearchResultsBatchSize"       = var.RESULTS_BATCH_SIZE

    "HlaMetadataDictionary:AzureStorageConnectionString"                          = var.azure_storage.primary_connection_string
    "HlaMetadataDictionary:SearchRelatedMetadata:CacheSlidingExpirationInSeconds" = var.SEARCH_RELATED_HLA_METADATA_CACHE_SLIDING_EXPIRATION_SEC

    "MacDictionary:AzureStorageConnectionString" = var.azure_storage.primary_connection_string
    "MacDictionary:TableName"                    = var.mac_import_table.name

    "MatchingConfiguration:MatchingBatchSize" = var.MATCHING_BATCH_SIZE

    "MessagingServiceBus:ConnectionString"                     = var.servicebus_namespace_authorization_rules.read-write.primary_connection_string
    "MessagingServiceBus:OriginalSearchRequestsSubscription"   = azurerm_servicebus_subscription.original-search-results-ready-repeat-search-listener.name
    "MessagingServiceBus:OriginalSearchRequestsTopic"          = var.original-search-matching-results-topic.name
    "MessagingServiceBus:RepeatSearchRequestsSubscription"     = azurerm_servicebus_subscription.repeat-search-repeat-search-requests.name
    "MessagingServiceBus:RepeatSearchRequestsMaxDeliveryCount" = azurerm_servicebus_subscription.repeat-search-repeat-search-requests.max_delivery_count
    "MessagingServiceBus:RepeatSearchRequestsTopic"            = azurerm_servicebus_topic.repeat-search-requests.name
    "MessagingServiceBus:RepeatSearchMatchingResultsTopic"     = azurerm_servicebus_topic.repeat-search-matching-results-ready.name
    "MessagingServiceBus:RepeatSearchResultsDebugSubscription" = azurerm_servicebus_subscription.debug-repeat-search-matching-results.name
    "MessagingServiceBus:SendRetryCount"                       = var.SEND_RETRY_COUNT
    "MessagingServiceBus:SendRetryCooldownSeconds"             = var.SEND_RETRY_COOLDOWN_SECONDS
    "NotificationsServiceBus:AlertsTopic"                      = var.servicebus_topics.alerts.name
    "NotificationsServiceBus:ConnectionString"                 = var.servicebus_namespace_authorization_rules.write-only.primary_connection_string
    "NotificationsServiceBus:NotificationsTopic"               = var.servicebus_topics.notifications.name
    "SearchTrackingServiceBus:ConnectionString"                = var.servicebus_namespace_authorization_rules.write-only.primary_connection_string
    "SearchTrackingServiceBus:SearchTrackingTopic"             = var.servicebus_topics.search_tracking.name

    // maximum running instances of the algorithm = maximum_worker_count * maxConcurrentCalls (in host.json).
    // together, alongside the non-repeat matching processes, these must ensure that the number of allowed concurrent SQL connections to the matching SQL DB is not exceeded.
    // See README_Integration.md for more details on concurrency configuration.
    "WEBSITE_MAX_DYNAMIC_APPLICATION_SCALE_OUT" = var.MAX_SCALE_OUT
    "WEBSITE_RUN_FROM_PACKAGE"                  = "1"
  }

  site_config {
    application_insights_key = var.application_insights.instrumentation_key
    application_stack {
      dotnet_version              = "v8.0"
      use_dotnet_isolated_runtime = true
    }
    cors {
      support_credentials = false
    }
    dynamic "ip_restriction" {
      for_each = var.IP_RESTRICTION_SETTINGS
      content {
        ip_address = ip_restriction
      }
    }

    health_check_path                 = "/api/HealthCheck"
    health_check_eviction_time_in_min = 10

    ftps_state              = "AllAllowed"
    scm_minimum_tls_version = "1.2"
  }

  connection_string {
    name  = "RepeatSearchSql"
    type  = "SQLAzure"
    value = local.repeat_search_database_connection_string
  }

  connection_string {
    name  = "MatchingPersistentSql"
    type  = "SQLAzure"
    value = var.matching_persistent_database_connection_string
  }

  connection_string {
    name  = "MatchingSqlA"
    type  = "SQLAzure"
    value = var.matching_transient_a_database_connection_string
  }

  connection_string {
    name  = "MatchingSqlB"
    type  = "SQLAzure"
    value = var.matching_transient_b_database_connection_string
  }

  connection_string {
    name  = "DonorSql"
    type  = "SQLAzure"
    value = var.donor_database_connection_string
  }
}
