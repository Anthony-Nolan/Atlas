resource "azurerm_service_plan" "atlas-elastic-plan" {
  name                         = "${local.environment}-ATLAS-ELASTIC-PLAN"
  location                     = local.location
  resource_group_name          = azurerm_resource_group.atlas_resource_group.name
  # maximum_elastic_worker_count = var.SERVICE_PLAN_MAX_SCALE_OUT
  lifecycle {
    create_before_destroy = true
    replace_triggered_by = [ null_resource.always_run ]
  }

  sku_name = var.SPIKE_SERVICE_PLAN
  os_type  = "Windows"
}

// independent service plan for public api - to ensure high availability even when the algorithm is doing a lot of work under high load.
resource "azurerm_service_plan" "atlas-public-api-elastic-plan" {
  name                         = "${local.environment}-ATLAS-PUBLIC-API-ELASTIC-PLAN"
  location                     = local.location
  resource_group_name          = azurerm_resource_group.atlas_resource_group.name
  maximum_elastic_worker_count = 5

  sku_name = "EP1"
  os_type  = "Windows"
}

resource "null_resource" "always_run" {
  triggers = {
    timestamp = "${timestamp()}"
  }
}
