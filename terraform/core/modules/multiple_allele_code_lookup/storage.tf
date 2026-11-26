resource "azurerm_storage_table" "multiple_allele_code_storage" {
  name               = "AtlasMultipleAlleleCodes"
  storage_account_id = var.azure_storage.id
}
