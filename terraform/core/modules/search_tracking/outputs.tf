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

// The connection string is exposed as a Key Vault reference rather than a value, so that consumers outside this module
// configure their apps without the secret passing through them. A consumer must attach the shared function apps
// identity for it to resolve.
output "sql_database" {
  value = {
    connection_string_kv_ref = local.sql_kv_ref["search-tracking-sql-connection-string"]
  }
}