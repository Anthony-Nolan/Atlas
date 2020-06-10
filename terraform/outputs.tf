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

output "resource_group" {
  value = {
    id = azurerm_resource_group.atlas_resource_group.id
  }
}

output "sql_server" {
  value = azurerm_sql_server.atlas_sql_server.name
}