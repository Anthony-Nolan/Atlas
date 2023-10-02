output "matching_algorithm_db_connection_string" {
  value = local.matching_algorithm_connection_string
}

output "matching_algorithm_validation_db_connection_string" {
  value = local.matching_algorithm_validation_connection_string
}

output "match_prediction_db_connection_string" {
  value = local.match_prediction_connection_string
}

output "donor_import_db_connection_string" {
  value = local.donor_import_connection_string
}

output "azure_storage_account_connection_string" {
  value     = azurerm_storage_account.system_test_storage.primary_connection_string
  sensitive = true
}

output "azure_app_configuration_connection_string" {
  value     = azurerm_app_configuration.atlas_test_config.primary_read_key[0].connection_string
  sensitive = true
}