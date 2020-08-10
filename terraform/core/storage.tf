resource "azurerm_storage_account" "function_storage" {
  name                      = "${lower(replace(local.environment, "/\\W/", ""))}atlasfuncstorage"
  resource_group_name       = azurerm_resource_group.atlas_resource_group.name
  location                  = var.LOCATION
  account_tier              = "Standard"
  account_replication_type  = "LRS"
  enable_https_traffic_only = true
  tags                      = local.common_tags
}

resource "azurerm_storage_account" "azure_storage" {
  name                      = "${lower(replace(local.environment, "/\\W/", ""))}atlasstorage"
  resource_group_name       = azurerm_resource_group.atlas_resource_group.name
  location                  = var.LOCATION
  account_tier              = "Standard"
  account_kind              = "StorageV2"
  account_replication_type  = "LRS"
  enable_https_traffic_only = true
  tags                      = local.common_tags
}

resource "azurerm_storage_container" "search_results_blob_container" {
  name                  = "atlas-search-results"
  storage_account_name  = azurerm_storage_account.azure_storage.name
  container_access_type = "private"
}