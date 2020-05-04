resource "azurerm_storage_account" "shared_function_storage" {
  name                      = "${lower(local.environment)}atlasfunctionstorage"
  resource_group_name       = azurerm_resource_group.atlas_resource_group.name
  location                  = var.LOCATION
  account_tier              = "Standard"
  account_replication_type  = "LRS"
  enable_https_traffic_only = true
  tags                      = local.common_tags
}

resource "azurerm_storage_account" "azure_storage" {
  name                      = "${lower(local.environment)}atlasstorage"
  resource_group_name       = azurerm_resource_group.atlas_resource_group.name
  location                  = var.LOCATION
  account_tier              = "Standard"
  account_replication_type  = "LRS"
  enable_https_traffic_only = true
  tags                      = local.common_tags
}

resource "azurerm_storage_container" "search_matching_results_blob_container" {
  name                 = "matching-algorithm-results"
  storage_account_name = azurerm_storage_account.azure_storage.name
  //  The following line is flagged as deprecated by IDE plugins, but is required in the currently used versions of terraform/azure provider 
  resource_group_name   = azurerm_resource_group.atlas_resource_group.name
  container_access_type = "private"
}

resource "azurerm_storage_container" "donor_import_blob_container" {
  name                  = "donor-import"
  storage_account_name  = azurerm_storage_account.azure_storage.name
//  The following line is flagged as deprecated by IDE plugins, but is required in the currently used versions of terraform/azure provider 
  resource_group_name   = azurerm_resource_group.atlas_resource_group.name
  container_access_type = "private"
}
