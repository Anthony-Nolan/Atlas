// This file is for outputs that are read by other terraform scripts as a remote state.
// If you want an output to be read by azure, use CIOutputs.tf

output "donor_import" {
  value = module.donor_import
}

output "match_prediction" {
  value = module.match_prediction
}

output "matching_algorithm" {
  value     = module.matching_algorithm
  sensitive = true
}

output "repeat_search" {
  value     = module.repeat_search
  sensitive = true
}

output "multiple_allele_code_lookup" {
  value = module.multiple_allele_code_lookup
}

output "public_api_function" {
  value = {
    api_key  = data.azurerm_function_app_host_keys.atlas_public_api_function_keys.default_function_key
    base_url = "https://${azurerm_windows_function_app.atlas_public_api_function.default_hostname}"
  }
  sensitive = true
}

output "resource_group_name" {
  value = local.resource_group_name
}

output "service_bus" {
  value = {
    namespace_name         = azurerm_servicebus_namespace.general.name
    read_connection_string = azurerm_servicebus_namespace_authorization_rule.read-only.primary_connection_string
  }
  sensitive = true
}

output "sql_server" {
  value = azurerm_mssql_server.atlas_sql_server.name
}

output "storage_account" {
  value = {
    id                = azurerm_storage_account.azure_storage.id
    connection_string = azurerm_storage_account.azure_storage.primary_connection_string
  }
  sensitive = true
}

output "support" {
  value = module.support
}
