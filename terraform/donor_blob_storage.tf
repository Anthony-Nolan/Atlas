resource "azurerm_storage_container" "donor_blob_storage" {
  name                  = "donorBlobStorage"
  storage_account_name  = azurerm_storage_account.azure_storage.name
  container_access_type = "private"
}
