resource "azurerm_storage_table" "atlas_multiple_allele_code_storage" {
  name                 = "${title(lower(local.environment))}AtlasMultipleAlleleCodeTable"
  storage_account_name = azurerm_storage_account.azure_storage.name
  resource_group_name  = azurerm_resource_group.atlas_resource_group.name
}
