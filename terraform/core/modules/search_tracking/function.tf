locals {
  atlas_search_tracking_function_name = "${var.general.environment}-ATLAS-SEARCH-TRACKING-FUNCTION"
}

resource "azurerm_windows_function_app" "atlas_search_tracking_function" {
  name                        = local.atlas_search_tracking_function_name
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

    "AzureFunctionsJobHost__extensions__serviceBus__messageHandlerOptions__maxConcurrentCalls" = 1

    "AzureAppConfiguration:ConnectionString" = var.azure_app_configuration.primary_read_key[0].connection_string

    "ConnectionStrings:PersistentSql" = local.search_tracking_database_connection_string

    "MessagingServiceBus:ConnectionString"           = var.servicebus_namespace_authorization_rules.read-write.primary_connection_string
    "MessagingServiceBus:SearchTrackingSubscription" = azurerm_servicebus_subscription.search-tracking.name
    "MessagingServiceBus:SearchTrackingTopic"        = azurerm_servicebus_topic.search-tracking-events.name

    "WEBSITE_RUN_FROM_PACKAGE" = "1"
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
    scm_minimum_tls_version = "1.0"
  }
}