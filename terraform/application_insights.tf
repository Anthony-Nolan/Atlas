resource "azurerm_application_insights" "atlas" {
  application_type    = "web"
  location            = local.location
  name                = "${local.environment}-ATLAS"
  resource_group_name = azurerm_resource_group.atlas_resource_group.name
}