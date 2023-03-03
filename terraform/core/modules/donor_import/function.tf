locals {
  donor_import_function_name = "${var.general.environment}-ATLAS-DONOR-IMPORT-FUNCTION"
}

resource "azurerm_windows_function_app" "atlas_donor_import_function" {
  name                        = local.donor_import_function_name
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

    "AzureStorage:ConnectionString"       = var.azure_storage.primary_connection_string,
    "AzureStorage:DonorFileBlobContainer" = azurerm_storage_container.donor_blob_storage.name
    "AzureStorage:DonorIdCheckerResultsBlobContainer" = local.donor_id_checker_results_container_name

    "DonorImport:FileCheckCronSchedule"    = var.STALLED_FILE_CHECK_CRONTAB
    "DonorImport:HoursToCheckStalledFiles" = var.STALLED_FILE_DURATION

    "MessagingServiceBus:ConnectionString"             = var.servicebus_namespace_authorization_rules.read-write.primary_connection_string
    "MessagingServiceBus:ImportFileSubscription"       = azurerm_servicebus_subscription.donor-import-file-processor.name
    "MessagingServiceBus:ImportFileTopic"              = azurerm_servicebus_topic.donor-import-file-uploads.name
    "MessagingServiceBus:UpdatedSearchableDonorsTopic" = azurerm_servicebus_topic.updated-searchable-donors.name
    "MessagingServiceBus:DonorIdCheckerTopic"          = azurerm_servicebus_topic.donor-id-checker-requests.name
    "MessagingServiceBus:DonorIdCheckerSubscription"   = azurerm_servicebus_subscription.donor-id-checker.name
    "MessagingServiceBus:DonorIdCheckerResultsTopic"   = azurerm_servicebus_topic.donor-id-checker-results.name

    "NotificationConfiguration:NotifyOnSuccessfulDonorImport"             = var.NOTIFICATIONS_ON_SUCCESSFUL_IMPORT
    "NotificationConfiguration:NotifyOnAttemptedDeletionOfUntrackedDonor" = var.NOTIFICATIONS_ON_DELETION_OF_INVALID_DONOR

    "NotificationsServiceBus:AlertsTopic"        = var.servicebus_topics.alerts.name
    "NotificationsServiceBus:ConnectionString"   = var.servicebus_namespace_authorization_rules.write-only.primary_connection_string
    "NotificationsServiceBus:NotificationsTopic" = var.servicebus_topics.notifications.name

    "PublishDonorUpdates:DeletionCronSchedule"        = var.DELETE_PUBLISHED_DONOR_UPDATES_CRONTAB
    "PublishDonorUpdates:PublishCronSchedule"         = var.PUBLISH_DONOR_UPDATES_CRONTAB
    "PublishDonorUpdates:PublishedUpdateExpiryInDays" = var.PUBLISHED_UPDATE_EXPIRY_IN_DAYS

    "WEBSITE_MAX_DYNAMIC_APPLICATION_SCALE_OUT" = var.MAX_INSTANCES
    "WEBSITE_RUN_FROM_PACKAGE"                  = "1"
  }

  site_config {
    application_insights_key = var.application_insights.instrumentation_key
    application_stack {
      dotnet_version = "6"
    }
    cors {
      allowed_origins     = []
      support_credentials = false
    }

    ip_restriction = [for ip in var.IP_RESTRICTION_SETTINGS : {
      ip_address = ip
      subnet_id  = null
    }]

    ftps_state              = "AllAllowed"
    scm_minimum_tls_version = "1.0"
  }

  connection_string {
    name  = "DonorStoreSql"
    type  = "SQLAzure"
    value = local.donor_import_connection_string
  }
}
