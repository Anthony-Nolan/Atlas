resource "azurerm_storage_container" "repeat_search_matching_results_container" {
  name                  = "repeat-search-matching-results"
  storage_account_id    = var.azure_storage.id
  container_access_type = "private"
}

resource "azurerm_storage_container" "repeat_search_results_container" {
  name                  = "repeat-search-results"
  storage_account_id    = var.azure_storage.id
  container_access_type = "private"
}