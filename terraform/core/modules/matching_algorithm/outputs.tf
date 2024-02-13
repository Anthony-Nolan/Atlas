output "azure_storage" {
  value = {
    search_results_container = azurerm_storage_container.search_matching_results_blob_container.name
  }
}

output "function_app" {
  value = {
    api_key                 = data.azurerm_function_app_host_keys.atlas_matching_algorithm_function_keys.default_function_key
    base_url                = "https://${azurerm_windows_function_app.atlas_matching_algorithm_function.default_hostname}"
    app_name                = local.matching_algorithm_temp_function_app_name
    donor_matching_app_name = local.donor_management_function_app_name
  }
}

output "service_bus" {
  value = {
    matching_requests_topic = azurerm_servicebus_topic.matching-requests
    matching_results_topic  = azurerm_servicebus_topic.matching-results-ready
  }
}

output "sql_database" {
  value = {
    sql_server                             = var.sql_server.fully_qualified_domain_name
    persistent_database_connection_string  = local.matching_persistent_database_connection_string
    transient_a_database_name              = azurerm_mssql_database.atlas-matching-transient-a.name
    transient_a_database_connection_string = local.matching_transient_database_a_connection_string
    transient_b_database_name              = azurerm_mssql_database.atlas-matching-transient-b.name
    transient_b_database_connection_string = local.matching_transient_database_b_connection_string
  }
}