

resource "azurerm_function_app" "atlas_donor_import_function" { 
  name                      = "${local.environment}-ATLAS-DONOR-IMPORT-FUNCTION"
  resource_group_name       = azurerm_resource_group.atlas_resource_group.name
  location                  = local.location
  app_service_plan_id       = azurerm_app_service_plan.atlas.id
  https_only                = true
  version                   = "~2"
  storage_connection_string = azurerm_storage_account.shared_function_storage.primary_connection_string

  tags = local.common_tags

  connection_string {
    name  = "PersistentSql"
    type  = "SQLAzure"
    value = local.data_refresh_persistent_connection_string
  }
}
