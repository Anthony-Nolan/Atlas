resource "azurerm_app_service_plan" "atlas" {
  name                = "${local.environment}-ATLAS"
  location            = local.location
  resource_group_name = azurerm_resource_group.atlas_resource_group.name

  sku {
    tier = var.SERVICE_PLAN_SKU["tier"]
    size = var.SERVICE_PLAN_SKU["size"]
  }
}

resource "azurerm_app_service_plan" "atlas-elastic-plan" {
  name                         = "${local.environment}-ATLAS-ELASTIC-PLAN"
  location                     = local.location
  resource_group_name          = azurerm_resource_group.atlas_resource_group.name
  kind                         = "elastic"
  maximum_elastic_worker_count = 50

  sku {
    tier = "ElasticPremium"
    size = "EP1"
  }
}

// independent service plan for public api - to ensure high availability even when the algorithm is doing a lot of work under high load.
resource "azurerm_app_service_plan" "atlas-public-api-elastic-plan" {
  name                         = "${local.environment}-ATLAS-PUBLIC-API-ELASTIC-PLAN"
  location                     = local.location
  resource_group_name          = azurerm_resource_group.atlas_resource_group.name
  kind                         = "elastic"
  maximum_elastic_worker_count = 5

  sku {
    tier = "ElasticPremium"
    size = "EP1"
  }
}