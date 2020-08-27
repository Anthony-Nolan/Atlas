locals {
  donor_import_function_name = "${var.general.environment}-ATLAS-DONOR-IMPORT-FUNCTION"
}

resource "azurerm_function_app" "atlas_donor_import_function" {
  name                       = local.donor_import_function_name
  resource_group_name        = var.app_service_plan.resource_group_name
  location                   = var.general.location
  app_service_plan_id        = var.app_service_plan.id
  https_only                 = true
  version                    = "~3"
  storage_account_access_key = var.shared_function_storage.primary_access_key
  storage_account_name       = var.shared_function_storage.name

  tags = var.general.common_tags

  app_settings = {
    "APPINSIGHTS_INSTRUMENTATIONKEY" = var.application_insights.instrumentation_key
    "ApplicationInsights:LogLevel"   = var.APPLICATION_INSIGHTS_LOG_LEVEL

    "AzureStorage:ConnectionString"       = var.azure_storage.primary_connection_string,
    "AzureStorage:DonorFileBlobContainer" = azurerm_storage_container.donor_blob_storage.name

    "DonorImport:FileCheckCronSchedule"    = var.STALLED_FILE_CHECK_CRONTAB
    "DonorImport:HoursToCheckStalledFiles" = var.STALLED_FILE_DURATION

    "FUNCTIONS_WORKER_RUNTIME" : "dotnet"

    "MessagingServiceBus:ConnectionString"         = var.servicebus_namespace_authorization_rules.read-write.primary_connection_string
    "MessagingServiceBus:ImportFileSubscription"   = azurerm_servicebus_subscription.donor-import-file-processor.name
    "MessagingServiceBus:ImportFileTopic"          = azurerm_servicebus_topic.donor-import-file-uploads.name
    "MessagingServiceBus:MatchingDonorUpdateTopic" = azurerm_servicebus_topic.updated-searchable-donors.name

    "NotificationsServiceBus:AlertsTopic"        = var.servicebus_topics.alerts.name
    "NotificationsServiceBus:ConnectionString"   = var.servicebus_namespace_authorization_rules.write-only.primary_connection_string
    "NotificationsServiceBus:NotificationsTopic" = var.servicebus_topics.notifications.name

    "WEBSITE_MAX_DYNAMIC_APPLICATION_SCALE_OUT" = "5"
    "WEBSITE_RUN_FROM_PACKAGE"                  = "1"
  }

  site_config {
    ip_restriction = [for ip in var.IP_RESTRICTION_SETTINGS : {
      ip_address = ip
      subnet_id  = null
    }]
  }

  connection_string {
    name  = "DonorStoreSql"
    type  = "SQLAzure"
    value = local.donor_import_connection_string
  }
}
