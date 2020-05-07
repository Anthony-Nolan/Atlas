output "matching_algorithm" {
  value = module.matching_algorithm.general
}

output "sql_server" {
  value = azurerm_sql_server.atlas_sql_server.name
}