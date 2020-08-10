resource "azurerm_storage_container" "donor_blob_storage" {
  name                  = "donors"
  storage_account_name  = var.azure_storage.name
  container_access_type = "private"
}
