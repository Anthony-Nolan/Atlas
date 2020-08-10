resource "azurerm_storage_table" "multiple_allele_code_storage" {
  name                 = "AtlasMultipleAlleleCodes"
  storage_account_name = var.azure_storage.name
}
