resource "azurerm_storage_account" "system_test_storage" {
  name                     = "${lower(replace(local.name_prefix, "/\\W/", ""))}systemteststorage"
  resource_group_name      = azurerm_resource_group.atlas_system_tests_resource_group.name
  min_tls_version          = "TLS1_0"
  location                 = local.location
  tags                     = local.common_tags
  account_tier             = "Standard"
  account_replication_type = "LRS"
}

resource "azurerm_storage_table" "multiple_allele_code_storage" {
  name                 = "AtlasMultipleAlleleCodes"
  storage_account_name = azurerm_storage_account.system_test_storage.name
}
