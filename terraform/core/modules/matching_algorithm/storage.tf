resource "azurerm_storage_container" "search_matching_results_blob_container" {
  name                 = "matching-algorithm-results"
  storage_account_name = var.azure_storage.name
  //  The following line is flagged as deprecated by IDE plugins, but is required in the currently used versions of terraform/azure provider 
  resource_group_name   = var.app_service_plan.resource_group_name
  container_access_type = "private"
}