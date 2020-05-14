resource "azurerm_storage_table" "atlas_multiple_allele_code_storage" {
  name                 = "AtlasMultipleAlleleCodes"
  storage_account_name = var.azure_storage.name
  resource_group_name  = var.app_service_plan.resource_group_name
}
