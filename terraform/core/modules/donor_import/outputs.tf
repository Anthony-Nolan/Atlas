output "function_app" {
  value = {
    hostname = azurerm_windows_function_app.atlas_donor_import_function.default_hostname
    app_name = local.donor_import_function_name
    id       = azurerm_windows_function_app.atlas_donor_import_function.id
  }
}

output "service_bus" {
  value = {
    updated_searchable_donors_topic = azurerm_servicebus_topic.updated-searchable-donors
  }
}

output "storage" {
  value = {
    donor_container_name = azurerm_storage_container.donor_blob_storage.name
  }
}

output "sql_database" {
  value = {
    connection_string = local.donor_import_connection_string
  }
}