resource "azurerm_storage_account" "shared_function_storage" {
  name                     = "${lower(local.environment)}atlasfunctionstorage"
  resource_group_name      = azurerm_resource_group.atlas_resource_group.name
  location                 = var.LOCATION
  account_tier             = var.FUNCTION_STORAGE_TIER
  account_replication_type = var.FUNCTION_STORAGE_REPLICATION_TYPE
  tags                     = local.common_tags
}

resource "azurerm_storage_account" "azure_storage" {
  name                     = "${lower(local.environment)}atlasstorage"
  resource_group_name      = azurerm_resource_group.atlas_resource_group.name
  location                 = var.LOCATION
  account_tier             = var.AZURE_STORAGE_TIER
  account_replication_type = var.AZURE_STORAGE_REPLICATION_TYPE
  tags                     = local.common_tags
}

resource "azurerm_storage_container" "search_matching_results_blob_container" {
  name                  = "matching-algorithm-results"
  storage_account_name  = azurerm_storage_account.azure_storage.name
  resource_group_name   = azurerm_resource_group.atlas_resource_group.name
  container_access_type = "private"
}
