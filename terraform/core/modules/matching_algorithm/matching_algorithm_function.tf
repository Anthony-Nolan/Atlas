locals {
  matching_func_app_settings = {
    "ApplicationInsights:LogLevel" = var.APPLICATION_INSIGHTS_LOG_LEVEL

    "AzureFunctionsJobHost__extensions__serviceBus__messageHandlerOptions__maxConcurrentCalls" = var.MAX_CONCURRENT_SERVICEBUS_FUNCTIONS

    "AzureManagement:Authentication:ClientId"     = var.AZURE_CLIENT_ID
    "AzureManagement:Authentication:ClientSecret" = var.AZURE_CLIENT_SECRET
    "AzureManagement:Authentication:OAuthBaseUrl" = var.AZURE_OAUTH_BASEURL
    "AzureManagement:Authentication:TenantId"     = var.AZURE_TENANT_ID

    "AzureManagement:Database:ServerName"                       = var.sql_server.name
    "AzureManagement:Database:PollingRetryIntervalMilliseconds" = var.DATABASE_OPERATION_POLLING_INTERVAL_MILLISECONDS
    "AzureManagement:Database:ResourceGroupName"                = var.resource_group.name
    "AzureManagement:Database:SubscriptionId"                   = var.general.subscription_id

    "AzureManagement:Monitoring:WorkspaceId" = var.application_insights_workspace.workspace_id

    "AzureAppConfiguration:ConnectionString" = var.azure_app_configuration.primary_read_key[0].connection_string

    "AzureStorage:ConnectionString"           = var.azure_storage.primary_connection_string
    "AzureStorage:SearchResultsBlobContainer" = azurerm_storage_container.search_matching_results_blob_container.name
    "AzureStorage:SearchResultsBatchSize"     = var.RESULTS_BATCH_SIZE

    "AzureWebJobs.GCCollect.Disabled" = var.MAINTENANCE_GCCOLLECT_DISABLED

    "DataRefresh:ActiveDatabaseAutoPauseTimeout"                                            = var.DATA_REFRESH_DB_AUTO_PAUSE_ACTIVE
    "DataRefresh:ActiveDatabaseSize"                                                        = var.DATA_REFRESH_DB_SIZE_ACTIVE
    "DataRefresh:AutoRunDataRefresh"                                                        = var.DATA_REFRESH_AUTO_RUN
    "DataRefresh:RequestsTopic"                                                             = azurerm_servicebus_topic.data-refresh-requests.name
    "DataRefresh:RequestsTopicSubscription"                                                 = azurerm_servicebus_subscription.matching-algorithm-data-refresh-requests.name
    "DataRefresh:CompletionTopic"                                                           = azurerm_servicebus_topic.completed-data-refresh-jobs.name
    "DataRefresh:CronTab"                                                                   = var.DATA_REFRESH_CRONTAB
    "DataRefresh:DatabaseAName"                                                             = azurerm_mssql_database.atlas-matching-transient-a.name
    "DataRefresh:DatabaseBName"                                                             = azurerm_mssql_database.atlas-matching-transient-b.name
    "DataRefresh:DataRefreshDonorUpdatesShouldBeFullyTransactional"                         = var.DONOR_WRITE_TRANSACTIONALITY__DATA_REFRESH
    "DataRefresh:DonorManagement:BatchSize"                                                 = var.MESSAGING_BUS_DONOR_BATCH_SIZE
    "DataRefresh:DonorManagement:CronSchedule"                                              = "NotActuallyUsedInThisFunction"
    "DataRefresh:DonorManagement:OngoingDifferentialDonorUpdatesShouldBeFullyTransactional" = var.DONOR_WRITE_TRANSACTIONALITY__DONOR_UPDATES
    "DataRefresh:DonorManagement:SubscriptionForDbA"                                        = azurerm_servicebus_subscription.matching_transient_a.name
    "DataRefresh:DonorManagement:SubscriptionForDbB"                                        = azurerm_servicebus_subscription.matching_transient_b.name
    "DataRefresh:DonorManagement:Topic"                                                     = var.servicebus_topics.updated-searchable-donors.name
    "DataRefresh:DormantDatabaseAutoPauseTimeout"                                           = var.DATA_REFRESH_DB_AUTO_PAUSE_DORMANT
    "DataRefresh:DormantDatabaseSize"                                                       = var.DATA_REFRESH_DB_SIZE_DORMANT
    "DataRefresh:RefreshDatabaseSize"                                                       = var.DATA_REFRESH_DB_SIZE_REFRESH

    "HlaMetadataDictionary:AzureStorageConnectionString"                          = var.azure_storage.primary_connection_string,
    "HlaMetadataDictionary:HlaNomenclatureSourceUrl"                              = var.WMDA_FILE_URL,
    "HlaMetadataDictionary:SearchRelatedMetadata:CacheSlidingExpirationInSeconds" = var.SEARCH_RELATED_HLA_METADATA_CACHE_SLIDING_EXPIRATION_SEC

    "MacDictionary:AzureStorageConnectionString" = var.azure_storage.primary_connection_string
    "MacDictionary:TableName"                    = var.mac_import_table.name,

    "Maintenance:GCCollect:CronSchedule" = var.MAINTENANCE_GCCOLLECT_CRON_SCHEDULE

    "MatchingConfiguration:MatchingBatchSize" = var.MATCHING_BATCH_SIZE,

    "MessagingServiceBus:ConnectionString"               = var.servicebus_namespace_authorization_rules.read-write.primary_connection_string
    "MessagingServiceBus:SearchRequestsMaxDeliveryCount" = azurerm_servicebus_subscription.matching-requests-matching-algorithm.max_delivery_count
    "MessagingServiceBus:SearchRequestsSubscription"     = azurerm_servicebus_subscription.matching-requests-matching-algorithm.name
    "MessagingServiceBus:SearchRequestsTopic"            = azurerm_servicebus_topic.matching-requests.name
    "MessagingServiceBus:SearchResultsTopic"             = azurerm_servicebus_topic.matching-results-ready.name
    "MessagingServiceBus:SearchResultsDebugSubscription" = azurerm_servicebus_subscription.debug-matching-results-ready.name

    "NotificationsServiceBus:ConnectionString"           = var.servicebus_namespace_authorization_rules.write-only.primary_connection_string
    "NotificationsServiceBus:AlertsTopic"                = var.servicebus_topics.alerts.name
    "NotificationsServiceBus:NotificationsTopic"         = var.servicebus_topics.notifications.name

    "SearchTrackingServiceBus:ConnectionString"          = var.servicebus_namespace_authorization_rules.write-only.primary_connection_string
    "SearchTrackingServiceBus:SearchTrackingTopic"       = var.servicebus_topics.search_tracking.name

    "Wmda:WmdaFileUri" = var.WMDA_FILE_URL

    "WEBSITE_RUN_FROM_PACKAGE"                  = var.WEBSITE_RUN_FROM_PACKAGE

    "WEBSITE_PROACTIVE_AUTOHEAL_ENABLED" = false
  }
  matching_algorithm_function_app_name = "${var.general.environment}-ATLAS-MATCHING-ALGORITHM-FUNCTIONS"
}

resource "azurerm_windows_function_app" "atlas_matching_algorithm_function" {
  name                        = local.matching_algorithm_function_app_name
  resource_group_name         = var.resource_group.name
  location                    = var.general.location
  service_plan_id             = var.elastic_app_service_plan.id
  client_certificate_mode     = "Required"
  https_only                  = true
  functions_extension_version = "~4"
  storage_account_access_key  = azurerm_storage_account.matching_function_storage.primary_access_key
  storage_account_name        = azurerm_storage_account.matching_function_storage.name

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
    app_scale_limit           = var.MAX_SCALE_OUT

    use_32_bit_worker       = false
    ftps_state              = "AllAllowed"
    scm_minimum_tls_version = "1.2"
  }

  tags = var.general.common_tags

  app_settings = local.matching_func_app_settings

  connection_string {
    name  = "SqlA"
    type  = "SQLAzure"
    value = local.matching_transient_database_a_connection_string
  }
  connection_string {
    name  = "SqlB"
    type  = "SQLAzure"
    value = local.matching_transient_database_b_connection_string
  }
  connection_string {
    name  = "PersistentSql"
    type  = "SQLAzure"
    value = local.matching_persistent_database_connection_string
  }

  connection_string {
    name  = "DonorSql"
    type  = "SQLAzure"
    value = "Server=tcp:${var.sql_server.fully_qualified_domain_name},1433;Initial Catalog=${var.donor_import_sql_database.name};Persist Security Info=False;User ID=${var.DONOR_IMPORT_DATABASE_USERNAME};Password=${var.DONOR_IMPORT_DATABASE_PASSWORD};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=1800;"
  }
}

data "azurerm_function_app_host_keys" "atlas_matching_algorithm_function_keys" {
  name                = azurerm_windows_function_app.atlas_matching_algorithm_function.name
  resource_group_name = var.resource_group.name
}
