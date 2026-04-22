data "azurerm_container_registry" "shared" {
  name                = var.ACR_NAME
  resource_group_name = var.ACR_RESOURCE_GROUP_NAME
}
