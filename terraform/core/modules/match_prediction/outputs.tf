output "function_app" {
  value = {
    hostname = azurerm_function_app.atlas_match_prediction_function.default_hostname
    app_name = local.atlas_match_prediction_function_name
    id       = azurerm_function_app.atlas_match_prediction_function.id
  }
}

output "storage" {
  value = {
    haplotype_frequency_set_container_name   = azurerm_storage_container.haplotype_frequency_set_blob_container.name
    match_prediction_results_container_name  = azurerm_storage_container.match_prediction_results_container.name
    match_prediction_requests_container_name = azurerm_storage_container.match_prediction_requests_container.name
  }
}

output "sql_database" {
  value = {
    connection_string = local.match_prediction_database_connection_string
  }
}