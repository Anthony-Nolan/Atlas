resource "azurerm_storage_account" "shared_function_storage" {
  name                     = "${lower(local.environment)}atlasfunctionstorage"
  resource_group_name      = azurerm_resource_group.atlas_resource_group.name
  location                 = var.location
  account_tier             = var.FUNCTION_STORAGE_TIER
  account_replication_type = var.FUNCTION_STORAGE_REPLICATION_TYPE
  tags                     = local.common_tags
}