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

    "AzureStorage:ConnectionString"                     = var.azure_storage.primary_connection_string,
    "AzureStorage:DonorFileBlobContainer"               = azurerm_storage_container.donor_blob_storage.name
    "AzureStorage:DonorIdCheckerResultsBlobContainer"   = local.donor_id_checker_results_container_name
    "AzureStorage:DonorInfoCheckerResultsBlobContainer" = local.donor_info_checker_results_container_name

    "DonorImport:FileCheckCronSchedule"    = var.STALLED_FILE_CHECK_CRONTAB
    "DonorImport:HoursToCheckStalledFiles" = var.STALLED_FILE_DURATION
    "DonorImport:AllowFullModeImport"      = var.ALLOW_FULL_MODE_IMPORT

    "MessagingServiceBus:ConnectionString"                    = var.servicebus_namespace_authorization_rules.read-write.primary_connection_string
    "MessagingServiceBus:ImportFileSubscription"              = azurerm_servicebus_subscription.donor-import-file-processor.name
    "MessagingServiceBus:ImportFileTopic"                     = azurerm_servicebus_topic.donor-import-file-uploads.name
    "MessagingServiceBus:UpdatedSearchableDonorsTopic"        = azurerm_servicebus_topic.updated-searchable-donors.name
    "MessagingServiceBus:DonorIdCheckerTopic"                 = azurerm_servicebus_topic.donor-id-checker-requests.name
    "MessagingServiceBus:DonorIdCheckerSubscription"          = azurerm_servicebus_subscription.donor-id-checker.name
    "MessagingServiceBus:DonorIdCheckerResultsTopic"          = azurerm_servicebus_topic.donor-id-checker-results.name
    "MessagingServiceBus:DonorInfoCheckerTopic"               = azurerm_servicebus_topic.donor-info-checker-requests.name
    "MessagingServiceBus:DonorInfoCheckerSubscription"        = azurerm_servicebus_subscription.donor-info-checker.name
    "MessagingServiceBus:DonorInfoCheckerResultsTopic"        = azurerm_servicebus_topic.donor-info-checker-results.name
    "MessagingServiceBus:DonorImportResultsTopic"             = azurerm_servicebus_topic.donor-import-results.name
    "MessagingServiceBus:DonorImportResultsDebugSubscription" = azurerm_servicebus_subscription.debug-donor-import-results.name
    "MessagingServiceBus:SendRetryCount"                      = var.SERVICE_BUS_SEND_RETRY_COUNT
    "MessagingServiceBus:SendRetryCooldownSeconds"            = var.SERVICE_BUS_SEND_RETRY_COOLDOWN_SECONDS

    "NotificationConfiguration:NotifyOnAttemptedDeletionOfUntrackedDonor" = var.NOTIFICATIONS_ON_DELETION_OF_INVALID_DONOR

    "NotificationsServiceBus:AlertsTopic"               = var.servicebus_topics.alerts.name
    "NotificationsServiceBus:ConnectionString"          = var.servicebus_namespace_authorization_rules.write-only.primary_connection_string
    "NotificationsServiceBus:NotificationsTopic"        = var.servicebus_topics.notifications.name
    "NotificationsServiceBus:SendRetryCount"            = var.SERVICE_BUS_SEND_RETRY_COUNT
    "NotificationsServiceBus:SendRetryCooldownSeconds"  = var.SERVICE_BUS_SEND_RETRY_COOLDOWN_SECONDS

    "PublishDonorUpdates:DeletionCronSchedule"        = var.DELETE_PUBLISHED_DONOR_UPDATES_CRONTAB
    "PublishDonorUpdates:PublishCronSchedule"         = var.PUBLISH_DONOR_UPDATES_CRONTAB
    "PublishDonorUpdates:PublishedUpdateExpiryInDays" = var.PUBLISHED_UPDATE_EXPIRY_IN_DAYS

    "FailureLogs:DeletionCronSchedule" = var.FAILURE_LOGS_CRONTAB
    "FailureLogs:ExpiryInDays"         = var.FAILURE_LOGS_EXPIRY_IN_DAYS

    "WEBSITE_MAX_DYNAMIC_APPLICATION_SCALE_OUT" = var.MAX_INSTANCES
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
    name  = "DonorStoreSql"
    type  = "SQLAzure"
    value = local.donor_import_connection_string
  }
}
