locals {
  atlas_repeat_search_function_name = "${var.general.environment}-ATLAS-REPEAT-SEARCH-FUNCTION"
}

resource "azurerm_function_app" "atlas_repeat_search_function" {
  name                       = local.atlas_repeat_search_function_name
  resource_group_name        = var.app_service_plan.resource_group_name
  location                   = var.general.location
  app_service_plan_id        = var.app_service_plan.id
  https_only                 = true
  version                    = "~3"
  storage_account_access_key = var.shared_function_storage.primary_access_key
  storage_account_name       = var.shared_function_storage.name

  tags = var.general.common_tags

  app_settings = {
    // APPINSIGHTS_INSTRUMENTATIONKEY
    //      The azure functions dashboard requires the instrumentation key with this name to integrate with application insights.
    "APPINSIGHTS_INSTRUMENTATIONKEY" = var.application_insights.instrumentation_key
    "ApplicationInsights:LogLevel"   = var.APPLICATION_INSIGHTS_LOG_LEVEL

    "FUNCTIONS_WORKER_RUNTIME" : "dotnet"
    "WEBSITE_RUN_FROM_PACKAGE" = "1"
  }

  site_config {
    ip_restriction = [for ip in var.IP_RESTRICTION_SETTINGS : {
      ip_address = ip
      subnet_id  = null
    }]
  }

  connection_string {
    name  = "RepeatSearchSql"
    type  = "SQLAzure"
    value = local.repeat_search_database_connection_string
  }
}
