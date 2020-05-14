output "general" {
  value = {
    base_url                  = "https://${azurerm_function_app.atlas_matching_algorithm_function.default_hostname}"
    api_key                   = var.FUNCTION_HOST_KEY
    persistent_database_name  = azurerm_sql_database.atlas-persistent.name
    transient_a_database_name = azurerm_sql_database.atlas-matching-transient-a.name
    transient_b_database_name = azurerm_sql_database.atlas-matching-transient-b.name
    search_requests_queue     = azurerm_servicebus_queue.matching-requests.name
    search_results_topic      = azurerm_servicebus_topic.matching-results-ready.name
  }
}