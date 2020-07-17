output "function_app" {
  value = {
    hostname = azurerm_function_app.atlas_donor_import_function.default_hostname
    app_name = local.donor_import_function_name
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
    name              = azurerm_sql_database.atlas-donor-import.name
    connection_string = local.donor_import_connection_string
  }
}