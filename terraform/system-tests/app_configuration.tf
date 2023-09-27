resource "azurerm_app_configuration" "atlas_test_config" {
  name                = "${lower(replace(local.name_prefix, "/\\W/", ""))}-system-tests-app-configuration"
  resource_group_name = azurerm_resource_group.atlas_system_tests_resource_group.name
  location            = local.location
  sku                 = "standard"
  tags                = local.common_tags

  lifecycle {
    prevent_destroy = true
  }
}

resource "azurerm_app_configuration_feature" "atlas_test_use_donor_info_stored_in_matching_algorithm_db" {
  configuration_store_id = azurerm_app_configuration.atlas_test_config.id
  description            = "Enables reading donor data from the matching database during matching phase"
  name                   = "useDonorInfoStoredInMatchingAlgorithmDb"
  enabled                = false
  lifecycle {
    ignore_changes = [enabled]
  }
}