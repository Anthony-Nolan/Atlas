resource "azurerm_application_insights" "atlas" {
  application_type    = "web"
  location            = local.location
  name                = "${local.environment}-ATLAS"
  resource_group_name = azurerm_resource_group.atlas_resource_group.name
  workspace_id        = azurerm_log_analytics_workspace.ai_workspace.id
}

resource "azurerm_log_analytics_workspace" "ai_workspace" {
  name                = "${local.environment}-ATLAS"
  location            = local.location
  resource_group_name = azurerm_resource_group.atlas_resource_group.name
}