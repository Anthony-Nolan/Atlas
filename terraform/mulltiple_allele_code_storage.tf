resource "azurerm_storage_table" "atlas_multiple_allele_code_storage" {
  name                 = lower("${local.environment}-ATLAS-MULTIPLE-ALLELE-CODE-TABLE")
  storage_account_name = azurerm_storage_account.azure_storage.name
  resource_group_name  = azurerm_resource_group.atlas_resource_group.name
}
