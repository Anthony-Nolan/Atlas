// shared storage account for non-performance critical functions apps.
// Documentation recommends a separate account for each functions app, for optimal performance - especially for durable functions 
// https://docs.microsoft.com/en-us/azure/azure-functions/storage-considerations 
resource "azurerm_storage_account" "function_storage" {
  // Lowercase only. Max = 24 characters, 8 reserved for environment
  name                            = "${lower(replace(local.environment, "/\\W/", ""))}atlasfuncstorage"
  resource_group_name             = azurerm_resource_group.atlas_resource_group.name
  location                        = var.LOCATION
  allow_nested_items_to_be_public = false
  account_tier                    = "Standard"
  account_replication_type        = "LRS"
  enable_https_traffic_only       = true
  min_tls_version                 = "TLS1_2"
  tags                            = local.common_tags
}

// standalone storage account for the top level "ATLAS-FUNCTIONS" function app 
resource "azurerm_storage_account" "atlas_durable_function_storage" {
  // Lowercase only. Max = 24 characters, 8 reserved for environment
  name                            = "${lower(replace(local.environment, "/\\W/", ""))}atlasdurablefunc"
  resource_group_name             = azurerm_resource_group.atlas_resource_group.name
  location                        = var.LOCATION
  allow_nested_items_to_be_public = false
  account_tier                    = "Standard"
  account_replication_type        = "LRS"
  enable_https_traffic_only       = true
  min_tls_version                 = "TLS1_2"
  tags                            = local.common_tags
}

resource "azurerm_storage_account" "azure_storage" {
  // Lowercase only. Max = 24 characters, 8 reserved for environment
  name                            = "${lower(replace(local.environment, "/\\W/", ""))}atlasstorage"
  resource_group_name             = azurerm_resource_group.atlas_resource_group.name
  location                        = var.LOCATION
  allow_nested_items_to_be_public = false
  account_tier                    = "Standard"
  account_kind                    = "StorageV2"
  account_replication_type        = "LRS"
  enable_https_traffic_only       = true
  min_tls_version                 = "TLS1_2"
  tags                            = local.common_tags
}

resource "azurerm_storage_container" "search_results_blob_container" {
  name                  = "atlas-search-results"
  storage_account_name  = azurerm_storage_account.azure_storage.name
  container_access_type = "private"
}