resource "azurerm_app_service_plan" "search_algorithm" {
  name                = "${local.environment}-SEARCH-ALGORITHM"
  location            = "${local.location}"
  resource_group_name = "${local.resource_group_name}"

  sku = {
    tier = "${var.SERVICE_PLAN_SKU["tier"]}"
    size = "${var.SERVICE_PLAN_SKU["size"]}"
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
    "apiKey:${var.APIKEY}"        = true
    "donorservice.apikey"         = "${var.DONORSERVICE_APIKEY}"
    "donorservice.baseurl"        = "${var.DONORSERVICE_BASEURL}"
    "hlaservice.apikey"           = "${var.HLASERVICE_APIKEY}"
    "hlaservice.baseurl"          = "${var.HLASERVICE_BASEURL}"
    "insights.instrumentationKey" = "${azurerm_application_insights.search_algorithm.instrumentation_key}"
  }

  connection_string = {
    name  = "HangfireSQLConnectionString"
    type  = "SQLAzure"
    value = "${var.CONNECTION_STRING_HANGFIRE}"
  }

  connection_string = {
    name  = "SQLConnectionString"
    type  = "SQLAzure"
    value = "${var.CONNECTION_STRING_SQL}"
  }

  connection_string = {
    name  = "StorageConnectionString"
    type  = "SQLAzure"
    value = "${var.CONNECTION_STRING_STORAGE}"
  }
}
