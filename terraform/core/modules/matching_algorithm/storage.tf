// Documentation recommends a separate account for each functions app, for optimal performance - especially for durable functions 
// https://docs.microsoft.com/en-us/azure/azure-functions/storage-considerations
// matching is performance critical, so use independent storage
resource "azurerm_storage_account" "matching_function_storage" {
  // Lowercase only. Max = 24 characters, 8 reserved for environment
  name                            = "${lower(replace(var.general.environment, "/\\W/", ""))}atlasmatchfunc"
  resource_group_name             = var.resource_group.name
  location                        = var.general.location
  account_tier                    = "Standard"
  account_replication_type        = "LRS"
  allow_nested_items_to_be_public = false
  enable_https_traffic_only       = true
  min_tls_version                 = "TLS1_0"
  tags                            = var.general.common_tags
}

resource "azurerm_storage_container" "search_matching_results_blob_container" {
  name                  = "matching-algorithm-results"
  storage_account_name  = var.azure_storage.name
  container_access_type = "private"
}