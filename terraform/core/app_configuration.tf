resource "azurerm_app_configuration" "atlas_config" {
  name                = "${local.environment}-ATLAS-APP-CONFIGURATION"
  resource_group_name = azurerm_resource_group.atlas_resource_group.name
  location            = local.location
  sku                 = "standard"
  tags                = local.common_tags

  lifecycle {
    prevent_destroy = true
  }
}