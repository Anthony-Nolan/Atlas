resource "azurerm_app_configuration_feature" "atlas_use_donor_info_stored_in_matching_algorithm_db" {
  configuration_store_id = var.azure_app_configuration.id
  description            = "Enables reading donor data from the matching database during matching phase"
  name                   = "useDonorInfoStoredInMatchingAlgorithmDb"
  enabled                = false
  lifecycle {
    ignore_changes = [enabled]
  }
}