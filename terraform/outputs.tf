output "matching_algorithm" {
  value = {
    base_url = "https://${azurerm_function_app.atlas_matching_algorithm_function.default_hostname}"
    api_key  = var.APIKEY
    persistent_database_name = azurerm_sql_database.atlas-persistent.name
    transient_a_database_name = azurerm_sql_database.atlas-matching-transient-a.name
    transient_b_database_name = azurerm_sql_database.atlas-matching-transient-b.name
  }
}

output "sql_server" {
  value = azurerm_sql_server.atlas_sql_server.name
}