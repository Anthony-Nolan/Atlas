resource "azurerm_service_plan" "atlas-elastic-plan" {
  name                         = "${local.environment}-ATLAS-ELASTIC-PLAN"
  location                     = local.location
  resource_group_name          = azurerm_resource_group.atlas_resource_group.name
  maximum_elastic_worker_count = var.ELASTIC_SERVICE_PLAN_MAX_SCALE_OUT

  sku_name = var.ELASTIC_SERVICE_PLAN_SKU_SIZE
  os_type  = "Windows"
}

// Independent service plan for public api to ensure high availability even when the algorithm is doing a lot of work under high load.
// Costs can be saved by configuring the release to not create this second service plan in environments where high availability is not needed, such as DEV/UAT.
resource "azurerm_service_plan" "atlas-public-api-elastic-plan" {
  count                        = var.ELASTIC_SERVICE_PLAN_FOR_PUBLIC_API ? 1 : 0
  name                         = "${local.environment}-ATLAS-PUBLIC-API-ELASTIC-PLAN"
  location                     = local.location
  resource_group_name          = azurerm_resource_group.atlas_resource_group.name
  maximum_elastic_worker_count = var.ELASTIC_SERVICE_PLAN_MAX_SCALE_OUT_PUBLIC_API

  sku_name = "EP1"
  os_type  = "Windows"
}