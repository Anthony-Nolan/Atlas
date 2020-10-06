resource "azurerm_storage_container" "haplotype_frequency_set_blob_container" {
  name                  = "haplotype-frequency-set-import"
  storage_account_name  = var.azure_storage.name
  container_access_type = "private"
}

resource "azurerm_storage_container" "match_prediction_results_container" {
  name                  = "match-prediction-results"
  storage_account_name  = var.azure_storage.name
  container_access_type = "private"
}