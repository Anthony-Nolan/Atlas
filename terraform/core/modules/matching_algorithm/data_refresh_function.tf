locals {
  # USE_EXTERNAL_SQL-aware Azure-management targets, so scale-up/scale-down
  # calls hit the same server/database the SqlA/SqlB connection strings do.
  data_refresh_database_a_name  = var.USE_EXTERNAL_SQL ? var.EXTERNAL_SQL_DB_MATCHING_A : azurerm_mssql_database.atlas-matching-transient-a.name
  data_refresh_database_b_name  = var.USE_EXTERNAL_SQL ? var.EXTERNAL_SQL_DB_MATCHING_B : azurerm_mssql_database.atlas-matching-transient-b.name
  data_refresh_sql_server_name  = var.USE_EXTERNAL_SQL ? var.EXTERNAL_SQL_SERVER_NAME : var.sql_server.name
  data_refresh_sql_rg_name      = var.USE_EXTERNAL_SQL ? var.EXTERNAL_SQL_RESOURCE_GROUP : var.resource_group.name
  data_refresh_sql_subscription = var.USE_EXTERNAL_SQL && var.EXTERNAL_SQL_SUBSCRIPTION_ID != "" ? var.EXTERNAL_SQL_SUBSCRIPTION_ID : var.general.subscription_id

  data_refresh_func_app_settings = {
    "ApplicationInsights:LogLevel" = var.APPLICATION_INSIGHTS_LOG_LEVEL

    "AutoMapper:LicenseKey" = var.AUTOMAPPER_LICENSE_KEY

    "AzureManagement:Authentication:ClientId"     = var.AZURE_CLIENT_ID
    "AzureManagement:Authentication:ClientSecret" = var.AZURE_CLIENT_SECRET
    "AzureManagement:Authentication:OAuthBaseUrl" = var.AZURE_OAUTH_BASEURL
    "AzureManagement:Authentication:TenantId"     = var.AZURE_TENANT_ID

    "AzureManagement:Database:ServerName"                       = local.data_refresh_sql_server_name
    "AzureManagement:Database:PollingRetryIntervalMilliseconds" = var.DATABASE_OPERATION_POLLING_INTERVAL_MILLISECONDS
    "AzureManagement:Database:ResourceGroupName"                = local.data_refresh_sql_rg_name
    "AzureManagement:Database:SubscriptionId"                   = local.data_refresh_sql_subscription

    "AzureStorage:ConnectionString" = var.azure_storage.primary_connection_string

    "DataRefresh:ActiveDatabaseAutoPauseTimeout"                                            = var.DATA_REFRESH_DB_AUTO_PAUSE_ACTIVE
    "DataRefresh:ActiveDatabaseSize"                                                        = var.DATA_REFRESH_DB_SIZE_ACTIVE
    "DataRefresh:AutoRunDataRefresh"                                                        = var.DATA_REFRESH_AUTO_RUN
    "DataRefresh:RequestsTopic"                                                             = azurerm_servicebus_topic.data-refresh-requests.name
    "DataRefresh:RequestsTopicSubscription"                                                 = azurerm_servicebus_subscription.matching-algorithm-data-refresh-requests.name
    "DataRefresh:CompletionTopic"                                                           = azurerm_servicebus_topic.completed-data-refresh-jobs.name
    "DataRefresh:CronTab"                                                                   = var.DATA_REFRESH_CRONTAB
    "DataRefresh:DatabaseAName"                                                             = local.data_refresh_database_a_name
    "DataRefresh:DatabaseBName"                                                             = local.data_refresh_database_b_name
    "DataRefresh:DataRefreshDonorUpdatesShouldBeFullyTransactional"                         = var.DONOR_WRITE_TRANSACTIONALITY__DATA_REFRESH
    "DataRefresh:DonorManagement:BatchSize"                                                 = var.MESSAGING_BUS_DONOR_BATCH_SIZE
    "DataRefresh:DonorManagement:CronSchedule"                                              = "NotActuallyUsedInThisFunction"
    "DataRefresh:DonorManagement:OngoingDifferentialDonorUpdatesShouldBeFullyTransactional" = var.DONOR_WRITE_TRANSACTIONALITY__DONOR_UPDATES
    "DataRefresh:DonorManagement:SubscriptionForDbA"                                        = azurerm_servicebus_subscription.matching_transient_a.name
    "DataRefresh:DonorManagement:SubscriptionForDbB"                                        = azurerm_servicebus_subscription.matching_transient_b.name
    "DataRefresh:DonorManagement:Topic"                                                     = var.servicebus_topics.updated-searchable-donors.name
    "DataRefresh:DormantDatabaseAutoPauseTimeout"                                           = var.DATA_REFRESH_DB_AUTO_PAUSE_DORMANT
    "DataRefresh:DormantDatabaseSize"                                                       = var.DATA_REFRESH_DB_SIZE_DORMANT
    "DataRefresh:LeaseDurationMinutes"                                                      = var.DATA_REFRESH_LEASE_DURATION_MINUTES
    "DataRefresh:LeaseRenewalIntervalSeconds"                                               = var.DATA_REFRESH_LEASE_RENEWAL_INTERVAL_SECONDS
    "DataRefresh:RefreshDatabaseSize"                                                       = var.DATA_REFRESH_DB_SIZE_REFRESH
    "DataRefresh:SendRetryCount"                                                            = var.SERVICE_BUS_SEND_RETRY_COUNT
    "DataRefresh:SendRetryCooldownSeconds"                                                  = var.SERVICE_BUS_SEND_RETRY_COOLDOWN_SECONDS

    "HlaMetadataDictionary:AzureStorageConnectionString"                          = var.azure_storage.primary_connection_string
    "HlaMetadataDictionary:HlaNomenclatureSourceUrl"                              = var.WMDA_FILE_URL
    "HlaMetadataDictionary:SearchRelatedMetadata:CacheSlidingExpirationInSeconds" = var.SEARCH_RELATED_HLA_METADATA_CACHE_SLIDING_EXPIRATION_SEC

    "MacDictionary:AzureStorageConnectionString" = var.azure_storage.primary_connection_string
    "MacDictionary:TableName"                    = var.mac_import_table.name

    "MessagingServiceBus:ConnectionString"         = var.servicebus_namespace_authorization_rules.read-write.primary_connection_string
    "MessagingServiceBus:SendRetryCount"           = var.SERVICE_BUS_SEND_RETRY_COUNT
    "MessagingServiceBus:SendRetryCooldownSeconds" = var.SERVICE_BUS_SEND_RETRY_COOLDOWN_SECONDS

    "NotificationsServiceBus:ConnectionString"         = var.servicebus_namespace_authorization_rules.write-only.primary_connection_string
    "NotificationsServiceBus:AlertsTopic"              = var.servicebus_topics.alerts.name
    "NotificationsServiceBus:NotificationsTopic"       = var.servicebus_topics.notifications.name
    "NotificationsServiceBus:SendRetryCount"           = var.SERVICE_BUS_SEND_RETRY_COUNT
    "NotificationsServiceBus:SendRetryCooldownSeconds" = var.SERVICE_BUS_SEND_RETRY_COOLDOWN_SECONDS

    "WEBSITE_RUN_FROM_PACKAGE" = var.WEBSITE_RUN_FROM_PACKAGE
  }

  data_refresh_function_app_name = "${var.general.environment}-DATA-REFRESH-FUNCTION"
}

resource "azurerm_windows_function_app" "atlas_matching_algorithm_data_refresh_function" {
  name                        = local.data_refresh_function_app_name
  resource_group_name         = var.elastic_app_service_plan.resource_group_name
  location                    = var.general.location
  service_plan_id             = var.elastic_app_service_plan.id
  client_certificate_mode     = "Required"
  https_only                  = true
  functions_extension_version = "~4"
  storage_account_access_key  = var.shared_function_storage.primary_access_key
  storage_account_name        = var.shared_function_storage.name

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

  tags = var.general.common_tags

  app_settings = local.data_refresh_func_app_settings

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
    value = local.matching_donor_database_connection_string
  }

  lifecycle {
    ignore_changes = [
      site_config[0].cors,
      tags["hidden-link: /app-insights-resource-id"],
    ]

    # Terraform is pinned to 1.4.0 in build-pipeline.yml, which predates
    # cross-variable variable{validation{}} blocks (needs 1.9+), so the
    # USE_EXTERNAL_SQL config guard lives here as a resource precondition
    # instead (supported since 1.2).
    precondition {
      condition = !var.USE_EXTERNAL_SQL || (
        var.EXTERNAL_SQL_SERVER_NAME != "" &&
        var.EXTERNAL_SQL_DB_SHARED != "" &&
        var.EXTERNAL_SQL_DB_MATCHING_A != "" &&
        var.EXTERNAL_SQL_DB_MATCHING_B != "" &&
        var.EXTERNAL_SQL_RESOURCE_GROUP != ""
      )
      error_message = "USE_EXTERNAL_SQL is true but one of EXTERNAL_SQL_SERVER_NAME, EXTERNAL_SQL_DB_SHARED, EXTERNAL_SQL_DB_MATCHING_A, EXTERNAL_SQL_DB_MATCHING_B or EXTERNAL_SQL_RESOURCE_GROUP is empty. All must be set when targeting an external SQL server."
    }
  }
}

data "azurerm_function_app_host_keys" "atlas_matching_algorithm_data_refresh_function_keys" {
  name                = azurerm_windows_function_app.atlas_matching_algorithm_data_refresh_function.name
  resource_group_name = var.elastic_app_service_plan.resource_group_name
}
