output "function_app" {
  value = {
    hostname = azurerm_windows_function_app.atlas_repeat_search_function.default_hostname
    app_name = local.atlas_repeat_search_function_name
    id       = azurerm_windows_function_app.atlas_repeat_search_function.id
  }
}

output "service_bus" {
  value = {
    repeat_search_matching_results_subscription = azurerm_servicebus_subscription.match-prediction-orchestration-repeat-search-results-ready
    repeat_search_matching_results_topic        = azurerm_servicebus_topic.repeat-search-matching-results-ready
    repeat_search_requests_topic                = azurerm_servicebus_topic.repeat-search-requests
    repeat_search_results_topic                 = azurerm_servicebus_topic.repeat-search-results-ready
    repeat_search_results_debug_subscription    = azurerm_servicebus_subscription.debug-repeat-search-results
  }
}

output "storage" {
  value = {
    repeat_search_matching_results_container_name = azurerm_storage_container.repeat_search_matching_results_container.name
    repeat_search_results_container_name          = azurerm_storage_container.repeat_search_results_container.name
  }
}

// The connection string is exposed as a Key Vault reference rather than a value, so that consumers outside this module
// configure their apps without the secret passing through them. A consumer must attach the shared function apps
// identity for it to resolve.
output "sql_database" {
  value = {
    connection_string_kv_ref = local.sql_kv_ref["repeat-search-sql-connection-string"]
  }
}
