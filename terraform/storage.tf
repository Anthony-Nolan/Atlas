resource "azurerm_storage_account" "shared_function_storage" {
  name                      = "${lower(local.environment)}atlasfunctionstorage"
  resource_group_name       = azurerm_resource_group.atlas_resource_group.name
  location                  = var.LOCATION
  account_tier              = var.FUNCTION_STORAGE_TIER
  account_replication_type  = var.FUNCTION_STORAGE_REPLICATION_TYPE
  enable_https_traffic_only = true
  tags                      = local.common_tags
}

resource "azurerm_storage_account" "DB_storage" {
  name                     = "${lower(local.environment)}atlasdbstorage"
  resource_group_name      = azurerm_resource_group.atlas_resource_group.name
  location                 = var.location
  account_tier             = var.DATA_REFRESH_DATABASE_REPLICATION_TIER
  account_replication_type = var.DATA_REFRESH_DATABASE_REPLICATION_TYPE
  tags                     = local.common_tags
}
resource "azurerm_storage_account" "azure_storage" {
  name                      = "${lower(local.environment)}atlasstorage"
  resource_group_name       = azurerm_resource_group.atlas_resource_group.name
  location                  = var.LOCATION
  account_tier              = var.AZURE_STORAGE_TIER
  account_replication_type  = var.AZURE_STORAGE_REPLICATION_TYPE
  enable_https_traffic_only = true
  tags                      = local.common_tags
}

resource "azurerm_storage_container" "search_matching_results_blob_container" {
  name                  = "matching-algorithm-results"
  storage_account_name  = azurerm_storage_account.azure_storage.name
//  The following line is flagged as deprecated by IDE plugins, but is required in the currently used versions of terraform/azure provider 
  resource_group_name   = azurerm_resource_group.atlas_resource_group.name
  container_access_type = "private"
}
