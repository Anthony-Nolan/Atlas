resource "azurerm_storage_container" "donor_blob_storage" {
  name                  = "donors"
  storage_account_name  = var.azure_storage.name
  container_access_type = "private"
  resource_group_name   = var.app_service_plan.resource_group_name
}
