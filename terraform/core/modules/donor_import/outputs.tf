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

// The connection string is exposed as a Key Vault reference rather than a value, so that consumers outside this module
// configure their apps without the secret passing through them. A consumer must attach the shared function apps
// identity for it to resolve.
output "sql_database" {
  value = {
    connection_string_kv_ref = local.sql_kv_ref["donor-import-sql-connection-string"]
  }
}