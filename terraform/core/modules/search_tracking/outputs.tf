output "function_app" {
  value = {
    hostname = azurerm_windows_function_app.atlas_search_tracking_function.default_hostname
    app_name = local.atlas_search_tracking_function_name
    id       = azurerm_windows_function_app.atlas_search_tracking_function.id
  }
}

output "service_bus" {
  value = {
    search_tracking_topic = azurerm_servicebus_topic.search-tracking-events
  }
}

output "sql_database" {
  value = {
    connection_string = local.search_tracking_database_connection_string
  }
}