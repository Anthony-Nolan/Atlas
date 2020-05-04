locals {
  matching_func_app_settings = {
    "ApplicationInsights:InstrumentationKey" = azurerm_application_insights.atlas.instrumentation_key
    //  The azure functions dashboard requires the instrumentation key with this name to integrate with application insights
    "APPINSIGHTS_INSTRUMENTATIONKEY"                            = azurerm_application_insights.atlas.instrumentation_key
    "ApplicationInsights:LogLevel"                              = var.APPLICATION_INSIGHTS_LOG_LEVEL
    "AzureManagement:Authentication:ClientId"                   = var.AZURE_CLIENT_ID
    "AzureManagement:Authentication:ClientSecret"               = var.AZURE_CLIENT_SECRET
    "AzureManagement:AppService:ResourceGroupName"              = azurerm_app_service_plan.atlas.resource_group_name
    "AzureManagement:AppService:SubscriptionId"                 = local.subscription_id
    "AzureManagement:Database:ServerName"                       = azurerm_sql_server.atlas_sql_server.name
    "AzureManagement:Database:PollingRetryIntervalMilliseconds" = var.MATCHING_DATABASE_OPERATITON_POLLING_INTERVAL_MILLISECONDS
    "AzureManagement:Database:ResourceGroupName"                = azurerm_resource_group.atlas_resource_group.name
    "AzureManagement:Database:SubscriptionId"                   = local.subscription_id
    "AzureStorage:ConnectionString"                             = azurerm_storage_account.azure_storage.primary_connection_string
    "AzureStorage:SearchResultsBlobContainer"                   = azurerm_storage_container.search_matching_results_blob_container.name
    "Client:DonorService:ApiKey"                                = var.DONOR_SERVICE_APIKEY
    "Client:DonorService:BaseUrl"                               = var.DONOR_SERVICE_BASEURL
    "Client:DonorService:ReadDonorsFromFile"                    = var.DONOR_SERVICE_READ_DONORS_FROM_FILE
    "Client:HlaService:ApiKey"                                  = var.HLA_SERVICE_APIKEY
    "Client:HlaService:BaseUrl"                                 = var.HLA_SERVICE_BASEURL
    "DataRefresh:ActiveDatabaseSize"                            = var.MATCHING_DATA_REFRESH_DB_SIZE_ACTIVE
    "DataRefresh:CronTab"                                       = var.MATCHING_DATA_REFRESH_CRONTAB
    "DataRefresh:DatabaseAName"                                 = azurerm_sql_database.atlas-matching-transient-a.name
    "DataRefresh:DatabaseBName"                                 = azurerm_sql_database.atlas-matching-transient-b.name
    "DataRefresh:DonorImportFunctionName"                       = var.MATCHING_DATA_REFRESH_DONOR_IMPORT_FUNCTION_NAME
    "DataRefresh:DonorFunctionsAppName"                         = azurerm_function_app.atlas_matching_algorithm_donor_management_function.name
    "DataRefresh:DormantDatabaseSize"                           = var.MATCHING_DATA_REFRESH_DB_SIZE_DORMANT
    "DataRefresh:RefreshDatabaseSize"                           = var.MATCHING_DATA_REFRESH_DB_SIZE_REFRESH
    // Historically this codebase used 2 distinct ServiceBusses; however we don't think that's necessary in practice.
    // We retain the ability to separate them again in the future, but in fact have them pointed at the same namespace
    // (albeit with different permissions)
    "MessagingServiceBus:ConnectionString"             = azurerm_servicebus_namespace_authorization_rule.read-write.primary_connection_string
    "MessagingServiceBus:DonorManagement.Topic"        = azurerm_servicebus_topic.updated-searchable-donors.name
    "MessagingServiceBus:DonorManagement.Subscription" = azurerm_servicebus_subscription.matching.name
    "MessagingServiceBus:SearchRequestsQueue"          = azurerm_servicebus_queue.matching-requests.name
    "MessagingServiceBus:SearchResultsTopic"           = azurerm_servicebus_topic.matching-results-ready.name
    "NotificationsServiceBus:ConnectionString"         = azurerm_servicebus_namespace_authorization_rule.write-only.primary_connection_string
    "NotificationsServiceBus:AlertsTopic"              = azurerm_servicebus_topic.alerts.name
    "NotificationsServiceBus:NotificationsTopic"       = azurerm_servicebus_topic.notifications.name
    "Wmda:WmdaFileUri"                                 = var.WMDA_FILE_URL
    "WEBSITE_MAX_DYNAMIC_APPLICATION_SCALE_OUT"        = "1"
    "WEBSITE_RUN_FROM_PACKAGE"                         = var.WEBSITE_RUN_FROM_PACKAGE
  }

  donor_func_app_settings = {
    "ApplicationInsights:InstrumentationKey" = azurerm_application_insights.atlas.instrumentation_key
    //  The azure functions dashboard requires the instrumentation key with this name to integrate with application insights
    "APPINSIGHTS_INSTRUMENTATIONKEY"                   = azurerm_application_insights.atlas.instrumentation_key
    "ApplicationInsights:LogLevel"                     = var.APPLICATION_INSIGHTS_LOG_LEVEL
    "AzureStorage:ConnectionString"                    = azurerm_storage_account.azure_storage.primary_connection_string
    "Client:HlaService:ApiKey"                         = var.HLA_SERVICE_APIKEY
    "Client:HlaService:BaseUrl"                        = var.HLA_SERVICE_BASEURL
    "MessagingServiceBus:ConnectionString"             = azurerm_servicebus_namespace_authorization_rule.read-only.primary_connection_string
    "MessagingServiceBus:DonorManagement:Topic"        = azurerm_servicebus_topic.updated-searchable-donors.name
    "MessagingServiceBus:DonorManagement:Subscription" = azurerm_servicebus_subscription.matching.name
    "MessagingServiceBus:DonorManagement:BatchSize"    = var.MATCHING_MESSAGING_BUS_DONOR_BATCH_SIZE
    "MessagingServiceBus:DonorManagement:CronSchedule" = var.MATCHING_MESSAGING_BUS_DONOR_CRON_SCHEDULE
    "MessagingServiceBus:SearchRequestsQueue"          = azurerm_servicebus_queue.matching-requests.name
    "MessagingServiceBus:SearchResultsTopic"           = azurerm_servicebus_topic.matching-results-ready.name
    "NotificationsServiceBus:ConnectionString"         = azurerm_servicebus_namespace_authorization_rule.write-only.primary_connection_string
    "NotificationsServiceBus:AlertsTopic"              = azurerm_servicebus_topic.alerts.name
    "NotificationsServiceBus:NotificationsTopic"       = azurerm_servicebus_topic.notifications.name
    "WEBSITE_MAX_DYNAMIC_APPLICATION_SCALE_OUT"        = "1"
    "WEBSITE_RUN_FROM_PACKAGE"                         = var.WEBSITE_RUN_FROM_PACKAGE
  }
}

resource "azurerm_function_app" "atlas_matching_algorithm_function" {
  name                      = "${local.environment}-ATLAS-MATCHING-ALGORITHM-FUNCTION"
  resource_group_name       = azurerm_resource_group.atlas_resource_group.name
  location                  = local.location
  app_service_plan_id       = azurerm_app_service_plan.atlas.id
  https_only                = true
  version                   = "~2"
  storage_connection_string = azurerm_storage_account.shared_function_storage.primary_connection_string

  site_config {
    always_on = true
  }

  tags = local.common_tags

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
}

resource "azurerm_function_app" "atlas_matching_algorithm_donor_management_function" {
  name                      = "${local.environment}-ATLAS-MATCHING-DONOR-MANAGEMENT-FUNCTION"
  resource_group_name       = azurerm_resource_group.atlas_resource_group.name
  location                  = local.location
  app_service_plan_id       = azurerm_app_service_plan.atlas.id
  https_only                = true
  version                   = "~2"
  storage_connection_string = azurerm_storage_account.shared_function_storage.primary_connection_string

  site_config {
    always_on = true
  }

  tags = local.common_tags

  app_settings = local.donor_func_app_settings

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
}

resource "azurerm_function_app" "atlas_donor_import_function" { 
  name                      = "${local.environment}-ATLAS-DONOT-IMPORT-FUNCTION"
  resource_group_name       = azurerm_resource_group.atlas_resource_group.name
  location                  = local.location
  app_service_plan_id       = azurerm_app_service_plan.atlas.id
  https_only                = true
  version                   = "~2"
  storage_connection_string = azurerm_storage_account.shared_function_storage.primary_connection_string

  site_config {
    always_on = true
  }

  tags = local.common_tags

  app_settings = local.donor_func_app_settings

  connection_string {
    name  = "PersistentSql"
    type  = "SQLAzure"
    value = local.data_refresh_persistent_connection_string
  }
}
