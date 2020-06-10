output "donor_import" {
  value = {
    function_app = module.donor_import.function_app
    service_bus  = module.donor_import.service_bus
    sql_database = module.donor_import.sql_database
    storage      = module.donor_import.storage
  }
}

output "matching_algorithm" {
  value = module.matching_algorithm.general
}

output "sql_server" {
  value = azurerm_sql_server.atlas_sql_server.name
}

output "storage_account" {
  value = {
    id = azurerm_storage_account.azure_storage.id
  }
}