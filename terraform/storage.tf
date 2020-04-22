resource "azurerm_storage_account" "shared_function_storage" {
  name                     = "${lower(local.environment)}atlasfunctionstorage"
  resource_group_name      = azurerm_resource_group.atlas_resource_group.name
  location                 = var.location
  account_tier             = var.FUNCTION_STORAGE_TIER
  account_replication_type = var.FUNCTION_STORAGE_REPLICATION_TYPE
  tags                     = local.common_tags
}

resource "azurerm_storage_account" "shared_azure_storage" {
  name                     = "${lower(local.environment)}atlasazurestorage"
  resource_group_name      = azurerm_resource_group.atlas_resource_group.name
  location                 = var.location
  account_tier             = var.AZURE_STORAGE_TIER
  account_replication_type = var.AZURE_STORAGE_REPLICATION_TYPE
  tags                     = local.common_tags
}

resource "azurerm_storage_container" "blob_container" {
  name                  = "${lower(local.environment)}atlasblobcontainer"
  storage_account_name  = azurerm_storage_account.shared_azure_storage.name
  container_access_type = "private"
}

resource "azurerm_storage_blob" "search_result_blob_storage" {
  name                   = "${lower(local.environment)}atlassearchresultblob"
  storage_account_name   = azurerm_storage_account.shared_azure_storage.name
  storage_container_name = azurerm_storage_container.blob_container.name
  type                   = "Block"
}

resource "azurerm_storage_table" "matching_dictionary_table" {
  name                 = "${lower(local.environment)}atlasmatchingdictionarytable"
  storage_account_name = azurerm_storage_account.shared_azure_storage.name
}
