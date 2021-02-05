resource "azurerm_storage_container" "repeat_search_matching_results_container" {
  name                  = "repeat-search-matching-results"
  storage_account_name  = var.azure_storage.name
  container_access_type = "private"
}

resource "azurerm_storage_container" "repeat_search_results_container" {
  name                  = "repeat-search-results"
  storage_account_name  = var.azure_storage.name
  container_access_type = "private"
}