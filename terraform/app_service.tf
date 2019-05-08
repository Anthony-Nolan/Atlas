resource "azurerm_app_service_plan" "search_algorithm" {
  name                = "${local.environment}-SEARCH-ALGORITHM"
  location            = "${local.location}"
  resource_group_name = "${local.resource_group_name}"

  sku = {
    tier = "${var.service-plan-sku["tier"]}"
    size = "${var.service-plan-sku["size"]}"
  }
}

resource "azurerm_application_insights" "search_algorithm" {
  application_type    = "web"
  location            = "${local.location}"
  name                = "${local.environment}-SEARCH-ALGORITHM"
  resource_group_name = "${local.resource_group_name}"
}

resource "azurerm_app_service" "search_algorithm" {
  name                = "${local.environment}-SEARCH-ALGORITHM-SERVICE"
  resource_group_name = "${local.resource_group_name}"
  location            = "${local.location}"
  app_service_plan_id = "${azurerm_app_service_plan.search_algorithm.id}"
  https_only          = true

  tags = {
    environment = "${local.environment}"
  }

  site_config = {
    always_on = true
  }

  app_settings = {
    "apiKey:${var.apiKey}"        = true
    "donorservice.apikey"         = "${var.donorservice_apiKey}"
    "donorservice.baseurl"        = "${var.donorservice_baseUrl}"
    "hlaservice.apikey"           = "${var.hlaservice_apiKey}"
    "hlaservice.baseurl"          = "${var.hlaservice_baseUrl}"
    "insights.instrumentationKey" = "${azurerm_application_insights.search_algorithm.instrumentation_key}"
  }

  connection_string = {
    name  = "HangfireSQLConnectionString"
    type  = "SQLAzure"
    value = "${var.connection_string_hangfire}"
  }

  connection_string = {
    name  = "SQLConnectionString"
    type  = "SQLAzure"
    value = "${var.connection_string_sql}"
  }

  connection_string = {
    name  = "StorageConnectionString"
    type  = "SQLAzure"
    value = "${var.connection_string_storage}"
  }
}
