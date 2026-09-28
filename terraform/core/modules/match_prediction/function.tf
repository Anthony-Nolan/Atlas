locals {
  atlas_match_prediction_function_name = "${var.general.environment}-ATLAS-MATCH-PREDICTION-FUNCTION"
}

resource "azurerm_windows_function_app" "atlas_match_prediction_function" {
  name                        = local.atlas_match_prediction_function_name
  resource_group_name         = var.app_service_plan.resource_group_name
  location                    = var.general.location
  service_plan_id             = var.app_service_plan.id
  client_certificate_mode     = "Required"
  https_only                  = true
  functions_extension_version = "~4"
  storage_account_access_key  = var.shared_function_storage.primary_access_key
  storage_account_name        = var.shared_function_storage.name

  // Used to resolve the @Microsoft.KeyVault app settings and connection strings below. The role assignment that lets
  // this identity read the vault lives in the root module, so it is ordered by the depends_on on the module block.
  identity {
    type         = "UserAssigned"
    identity_ids = [var.key_vault.function_apps_identity_id]
  }

  key_vault_reference_identity_id = var.key_vault.function_apps_identity_id

  tags = var.general.common_tags

  app_settings = {
    "ApplicationInsights:LogLevel" = var.APPLICATION_INSIGHTS_LOG_LEVEL

    "AzureStorage:ConnectionString"                    = var.key_vault.secret_refs["azure-storage-connection-string"]
    "AzureStorage:MatchPredictionResultsBlobContainer" = azurerm_storage_container.match_prediction_results_container.name

    "HlaMetadataDictionary:AzureStorageConnectionString"                          = var.key_vault.secret_refs["azure-storage-connection-string"]
    "HlaMetadataDictionary:SearchRelatedMetadata:CacheSlidingExpirationInSeconds" = var.SEARCH_RELATED_HLA_METADATA_CACHE_SLIDING_EXPIRATION_SEC
    "HaplotypeFrequencySetCache:ActiveSetCacheExpiryMinutes"                      = var.ACTIVE_HF_SET_CACHE_EXPIRY_MINUTES
    "HaplotypeFrequencySetCache:MaxCachedFrequencySets"                           = var.MAX_CACHED_FREQUENCY_SETS
    "HaplotypeFrequencySetCache:SetCacheExpiryMinutes"                            = var.SET_CACHE_EXPIRY_MINUTES

    "GenotypeImputation:MaximumExpandedGenotypesPerInput" = var.MATCH_PREDICTION_MAX_EXPANDED_GENOTYPES_PER_INPUT

    "MacDictionary:AzureStorageConnectionString" = var.key_vault.secret_refs["azure-storage-connection-string"]
    "MacDictionary:TableName"                    = var.mac_import_table.name,

    "MessagingServiceBus:ConnectionString"         = var.key_vault.secret_refs["servicebus-manage-connection-string"]
    "MessagingServiceBus:ImportFileSubscription"   = azurerm_servicebus_subscription.haplotype-frequency-file-processor.name
    "MessagingServiceBus:ImportFileTopic"          = azurerm_servicebus_topic.haplotype-frequency-file-uploads.name
    "MessagingServiceBus:SendRetryCount"           = var.SERVICE_BUS_SEND_RETRY_COUNT
    "MessagingServiceBus:SendRetryCooldownSeconds" = var.SERVICE_BUS_SEND_RETRY_COOLDOWN_SECONDS

    "MatchPredictionRequests:RequestsSubscription" = azurerm_servicebus_subscription.match-prediction-request-runner.name
    "MatchPredictionRequests:RequestsTopic"        = azurerm_servicebus_topic.match-prediction-requests.name
    "MatchPredictionRequests:ResultsTopic"         = azurerm_servicebus_topic.match-prediction-results.name
    "MatchPredictionRequests:MaxParallelism"       = var.MATCH_PREDICTION_REQUESTS_MAX_PARALLELISM

    "NotificationsServiceBus:ConnectionString"         = var.key_vault.secret_refs["servicebus-write-only-connection-string"]
    "NotificationsServiceBus:AlertsTopic"              = var.servicebus_topics.alerts.name
    "NotificationsServiceBus:NotificationsTopic"       = var.servicebus_topics.notifications.name
    "NotificationsServiceBus:SendRetryCount"           = var.SERVICE_BUS_SEND_RETRY_COUNT
    "NotificationsServiceBus:SendRetryCooldownSeconds" = var.SERVICE_BUS_SEND_RETRY_COOLDOWN_SECONDS

    "WEBSITE_RUN_FROM_PACKAGE" = var.WEBSITE_RUN_FROM_PACKAGE
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

    pre_warmed_instance_count = 1
    app_scale_limit           = 1

    ftps_state              = "AllAllowed"
    scm_minimum_tls_version = "1.2"
  }

  connection_string {
    name  = "MatchPredictionSql"
    type  = "SQLAzure"
    value = local.sql_kv_ref["match-prediction-sql-connection-string"]
  }

  lifecycle {
    ignore_changes = [
      site_config[0].health_check_eviction_time_in_min,
      site_config[0].cors,
      tags["hidden-link: /app-insights-resource-id"],
    ]
  }
}
