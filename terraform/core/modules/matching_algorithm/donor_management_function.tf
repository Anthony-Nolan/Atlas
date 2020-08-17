locals {
  donor_func_app_settings = {
    // APPINSIGHTS_INSTRUMENTATIONKEY
    //      The azure functions dashboard requires the instrumentation key with this name to integrate with application insights.
    "APPINSIGHTS_INSTRUMENTATIONKEY" = var.application_insights.instrumentation_key
    "ApplicationInsights:LogLevel"   = var.APPLICATION_INSIGHTS_LOG_LEVEL

    "AzureStorage:ConnectionString" = var.azure_storage.primary_connection_string

    "FUNCTIONS_WORKER_RUNTIME" : "dotnet"

    "HlaMetadataDictionary:AzureStorageConnectionString" = var.azure_storage.primary_connection_string,

    "MacDictionary:AzureStorageConnectionString" = var.azure_storage.primary_connection_string
    "MacDictionary:TableName"                    = var.mac_import_table.name,

    "MessagingServiceBus:ConnectionString"                                                          = var.servicebus_namespace_authorization_rules.read-only.primary_connection_string
    "MessagingServiceBus:DonorManagement:Topic"                                                     = var.servicebus_topics.updated-searchable-donors.name
    "MessagingServiceBus:DonorManagement:SubscriptionForDbA"                                        = azurerm_servicebus_subscription.matching_transient_a.name
    "MessagingServiceBus:DonorManagement:SubscriptionForDbB"                                        = azurerm_servicebus_subscription.matching_transient_b.name
    "MessagingServiceBus:DonorManagement:BatchSize"                                                 = var.MESSAGING_BUS_DONOR_BATCH_SIZE
    "MessagingServiceBus:DonorManagement:CronSchedule"                                              = var.MESSAGING_BUS_DONOR_CRON_SCHEDULE
    "MessagingServiceBus:DonorManagement:OngoingDifferentialDonorUpdatesShouldBeFullyTransactional" = var.DONOR_WRITE_TRANSACTIONALITY__DONOR_UPDATES

    "NotificationsServiceBus:ConnectionString"   = var.servicebus_namespace_authorization_rules.write-only.primary_connection_string
    "NotificationsServiceBus:AlertsTopic"        = var.servicebus_topics.alerts.name
    "NotificationsServiceBus:NotificationsTopic" = var.servicebus_topics.notifications.name

    "WEBSITE_MAX_DYNAMIC_APPLICATION_SCALE_OUT" = "1"
    "WEBSITE_RUN_FROM_PACKAGE"                  = var.WEBSITE_RUN_FROM_PACKAGE
  }
  donor_management_function_app_name = "${var.general.environment}-ATLAS-MATCHING-DONOR-MANAGEMENT-FUNCTION"
}

resource "azurerm_function_app" "atlas_matching_algorithm_donor_management_function" {
  name                      = local.donor_management_function_app_name
  resource_group_name       = var.app_service_plan.resource_group_name
  location                  = var.general.location
  app_service_plan_id       = var.app_service_plan.id
  https_only                = true
  version                   = "~3"
  storage_connection_string = var.shared_function_storage.primary_connection_string

  site_config {
    always_on = true
    ip_restriction = [for ip in var.IP_RESTRICTION_SETTINGS : {
      ip_address = ip
      subnet_id  = null
    }]
  }

  tags = var.general.common_tags

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
