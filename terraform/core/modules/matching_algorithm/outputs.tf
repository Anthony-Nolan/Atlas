output "azure_storage" {
  value = {
    search_results_container = azurerm_storage_container.search_matching_results_blob_container.name
  }
}

output "function_app" {
  value = {
    api_key                 = var.FUNCTION_HOST_KEY
    base_url                = "https://${azurerm_function_app.atlas_matching_algorithm_function.default_hostname}"
    app_name                = local.matching_algorithm_function_app_name
    donor_matching_app_name = local.donor_management_function_app_name
  }
}

output "service_bus" {
  value = {
    search_requests_queue  = azurerm_servicebus_queue.matching-requests.name
    matching_results_topic = azurerm_servicebus_topic.matching-results-ready.name
  }
}

output "sql_database" {
  value = {
    sql_server                             = var.sql_server.fully_qualified_domain_name
    persistent_database_connection_string  = local.matching_persistent_database_connection_string
    transient_a_database_name              = azurerm_sql_database.atlas-matching-transient-a.name
    transient_a_database_connection_string = local.matching_transient_database_a_connection_string
    transient_b_database_name              = azurerm_sql_database.atlas-matching-transient-b.name
    transient_b_database_connection_string = local.matching_transient_database_b_connection_string
  }
}