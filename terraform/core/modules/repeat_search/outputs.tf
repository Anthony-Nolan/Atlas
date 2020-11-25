output "function_app" {
  value = {
    hostname = azurerm_function_app.atlas_repeat_search_function.default_hostname
    app_name = local.atlas_repeat_search_function_name
    id       = azurerm_function_app.atlas_repeat_search_function.id
  }
}

output "storage" {
  value = {
    repeat_search_results_container_name = azurerm_storage_container.repeat_search_results_container.name
  }
}

output "sql_database" {
  value = {
    connection_string = local.repeat_search_database_connection_string
  }
}