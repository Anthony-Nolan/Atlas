locals {
  func_app_settings = {
    "ApplicationInsights.InstrumentationKey"                    = azurerm_application_insights.atlas.instrumentation_key
    //  The azure functions dashboard requires the instrumentation key with this name to integrate with application insights
    "APPINSIGHTS_INSTRUMENTATIONKEY"                            = azurerm_application_insights.atlas.instrumentation_key
    "ApplicationInsights.LogLevel"                              = var.APPLICATION_INSIGHTS_LOG_LEVEL
    "AzureManagement.Authentication.ClientId"                   = var.AZURE_CLIENT_ID
    "AzureManagement.Authentication.ClientSecret"               = var.AZURE_CLIENT_SECRET
    "AzureManagement.AppService.ResourceGroupName"              = azurerm_app_service_plan.atlas.resource_group_name
    "AzureManagement.AppService.SubscriptionId"                 = var.FUNCTION_APP_SUBSCRIPTION_ID
    "AzureManagement.Database.ServerName"                       = var.DATABASE_SERVER_NAME
    "AzureManagement.Database.PollingRetryIntervalMilliseconds" = var.DATABASE_OPERATITON_POLLING_INTERVAL_MILLISECONDS
    "AzureManagement.Database.ResourceGroupName"                = var.DATABASE_RESOURCE_GROUP
    "AzureManagement.Database.SubscriptionId"                   = var.DATABASE_SUBSCRIPTION_ID
    "AzureStorage.ConnectionString"                             = azurerm_storage_account.azure_storage.primary_connection_string
    "AzureStorage.SearchResultsBlobContainer"                   = azurerm_storage_container.search_matching_results_blob_container.name
    "Client.DonorService.ApiKey"                                = var.DONOR_SERVICE_APIKEY
    "Client.DonorService.BaseUrl"                               = var.DONOR_SERVICE_BASEURL
    "Client.HlaService.ApiKey"                                  = var.HLA_SERVICE_APIKEY
    "Client.HlaService.BaseUrl"                                 = var.HLA_SERVICE_BASEURL
    "DataRefresh.ActiveDatabaseSize"                            = var.DATA_REFRESH_DB_SIZE_ACTIVE
    "DataRefresh.CronTab"                                       = var.DATA_REFRESH_CRONTAB
    "DataRefresh.DatabaseAName"                                 = var.DATA_REFRESH_DATABASE_A_NAME
    "DataRefresh.DatabaseBName"                                 = var.DATA_REFRESH_DATABASE_B_NAME
    "DataRefresh.DonorImportFunctionName"                       = var.DATA_REFRESH_DONOR_IMPORT_FUNCTION_NAME
    "DataRefresh.DonorFunctionsAppName"                         = azurerm_function_app.atlas_matching_algorithm_donor_management_function.name
    "DataRefresh.DormantDatabaseSize"                           = var.DATA_REFRESH_DB_SIZE_DORMANT
    "DataRefresh.RefreshDatabaseSize"                           = var.DATA_REFRESH_DB_SIZE_REFRESH
    "MessagingServiceBus.ConnectionString"                      = var.MESSAGING_BUS_CONNECTION_STRING
    "MessagingServiceBus.DonorManagement.Topic"                 = var.MESSAGING_BUS_DONOR_TOPIC
    "MessagingServiceBus.DonorManagement.Subscription"          = var.MESSAGING_BUS_DONOR_SUBSCRIPTION
    "MessagingServiceBus.SearchRequestsQueue"                   = var.MESSAGING_BUS_SEARCH_REQUESTS_QUEUE
    "MessagingServiceBus.SearchResultsTopic"                    = var.MESSAGING_BUS_SEARCH_RESULTS_TOPIC
    "NotificationsServiceBus.AlertsTopic"                       = var.NOTIFICATIONS_BUS_ALERTS_TOPIC
    "NotificationsServiceBus.ConnectionString"                  = var.NOTIFICATIONS_BUS_CONNECTION_STRING
    "NotificationsServiceBus.NotificationsTopic"                = var.NOTIFICATIONS_BUS_NOTIFICATIONS_TOPIC
    "Wmda.WmdaFileUri"                                          = var.WMDA_FILE_URL
    "WEBSITE_MAX_DYNAMIC_APPLICATION_SCALE_OUT"                 = "1"
    "WEBSITE_RUN_FROM_PACKAGE"                                  = var.WEBSITE_RUN_FROM_PACKAGE                
  }

  donor_func_app_settings = {
    "ApplicationInsights.InstrumentationKey"           = azurerm_application_insights.atlas.instrumentation_key
    //  The azure functions dashboard requires the instrumentation key with this name to integrate with application insights
    "APPINSIGHTS_INSTRUMENTATIONKEY"                   = azurerm_application_insights.atlas.instrumentation_key
    "ApplicationInsights.LogLevel"                     = var.APPLICATION_INSIGHTS_LOG_LEVEL
    "AzureStorage.ConnectionString"                    = azurerm_storage_account.azure_storage.primary_connection_string
    "Client.HlaService.ApiKey"                         = var.HLA_SERVICE_APIKEY
    "Client.HlaService.BaseUrl"                        = var.HLA_SERVICE_BASEURL
    "MessagingServiceBus.ConnectionString"             = var.MESSAGING_BUS_CONNECTION_STRING
    "MessagingServiceBus.DonorManagement.BatchSize"    = var.MESSAGING_BUS_DONOR_BATCH_SIZE
    "MessagingServiceBus.DonorManagement.CronSchedule" = var.MESSAGING_BUS_DONOR_CRON_SCHEDULE
    "MessagingServiceBus.DonorManagement.Topic"        = var.MESSAGING_BUS_DONOR_TOPIC
    "MessagingServiceBus.DonorManagement.Subscription" = var.MESSAGING_BUS_DONOR_SUBSCRIPTION
    "NotificationsServiceBus.AlertsTopic"              = var.NOTIFICATIONS_BUS_ALERTS_TOPIC
    "NotificationsServiceBus.ConnectionString"         = var.NOTIFICATIONS_BUS_CONNECTION_STRING
    "NotificationsServiceBus.NotificationsTopic"       = var.NOTIFICATIONS_BUS_NOTIFICATIONS_TOPIC	
    "WEBSITE_MAX_DYNAMIC_APPLICATION_SCALE_OUT"        = "1"
	"WEBSITE_RUN_FROM_PACKAGE"                         = var.WEBSITE_RUN_FROM_PACKAGE
  }
}

resource "azurerm_function_app" "atlas_matching_algorithm_function" {
  name                      = "${local.environment}-ATLAS-MACTHING-ALGORITHM-FUNCTION"
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

  app_settings = local.func_app_settings

  connection_string {
    name  = "SqlA"
    type  = "SQLAzure"
    value = var.CONNECTION_STRING_SQL_A
  }
  connection_string {
    name  = "SqlB"
    type  = "SQLAzure"
    value = var.CONNECTION_STRING_SQL_B
  }
  connection_string {
    name  = "PersistentSql"
    type  = "SQLAzure"
    value = var.CONNECTION_STRING_SQL_PERSISTENT
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
    value = var.CONNECTION_STRING_SQL_A
  }
  connection_string {
    name  = "SqlB"
    type  = "SQLAzure"
    value = var.CONNECTION_STRING_SQL_B
  }
  connection_string {
    name  = "PersistentSql"
    type  = "SQLAzure"
    value = var.CONNECTION_STRING_SQL_PERSISTENT
  }
}
