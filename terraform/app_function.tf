locals {
  func_app_settings = {
    "Client.DonorService.ApiKey"                = data.terraform_remote_state.donor.outputs.donor_service.api_key
    "Client.DonorService.BaseUrl"               = data.terraform_remote_state.donor.outputs.donor_service.base_url
    "Client.HlaService.ApiKey"                  = data.terraform_remote_state.hla.outputs.hla_service.api_key
    "Client.HlaService.BaseUrl"                 = data.terraform_remote_state.hla.outputs.hla_service.base_url
    "ApplicationInsights.InstrumentationKey"    = azurerm_application_insights.search_algorithm.instrumentation_key
    "AzureStorage.ConnectionString"             = var.CONNECTION_STRING_STORAGE
    "WEBSITE_MAX_DYNAMIC_APPLICATION_SCALE_OUT" = "1"
  }
}

resource "azurerm_app_service_plan" "search_algorithm" {
  name                = "${local.environment}-SEARCH-ALGORITHM"
  location            = local.location
  resource_group_name = local.resource_group_name

  sku {
    tier = var.SERVICE_PLAN_SKU["tier"]
    size = var.SERVICE_PLAN_SKU["size"]
  }
}

resource "azurerm_application_insights" "search_algorithm" {
  application_type    = "web"
  location            = local.location
  name                = "${local.environment}-SEARCH-ALGORITHM"
  resource_group_name = local.resource_group_name
}

resource "azurerm_function_app" "search-algorithm_function" {
  name                      = "${local.environment}-NOVA-SEARCH-ALGORITHM-FUNCTION"
  resource_group_name       = local.resource_group_name
  location                  = local.location
  app_service_plan_id       = azurerm_app_service_plan.search_algorithm.id
  https_only                = true
  version                   = "~2"
  storage_connection_string = local.function_storage_connection_string

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

