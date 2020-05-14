resource "azurerm_function_app" "atlas_donor_import_function" {
  name                      = "${var.general.environment}-ATLAS-DONOR-IMPORT-FUNCTION"
  resource_group_name       = var.app_service_plan.resource_group_name
  location                  = var.general.location
  app_service_plan_id       = var.app_service_plan.id
  https_only                = true
  version                   = "~3"
  storage_connection_string = var.shared_function_storage.primary_connection_string

  tags = var.general.common_tags

  connection_string {
    name  = "Sql"
    type  = "SQLAzure"
    value = local.donor_import_connection_string
  }
}
