output "general" {
  value = {
    base_url                  = "https://${azurerm_function_app.atlas_matching_algorithm_function.default_hostname}"
    api_key                   = var.MATCHING_FUNCTION_HOST_KEY
    persistent_database_name  = azurerm_sql_database.atlas-persistent.name
    transient_a_database_name = azurerm_sql_database.atlas-matching-transient-a.name
    transient_b_database_name = azurerm_sql_database.atlas-matching-transient-b.name
  }
}