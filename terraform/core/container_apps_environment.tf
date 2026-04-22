resource "azurerm_container_app_environment" "atlas" {
  name                       = "${local.environment}-ATLAS-CONTAINER-APPS-ENV"
  location                   = local.location
  resource_group_name        = azurerm_resource_group.atlas_resource_group.name
  log_analytics_workspace_id = azurerm_log_analytics_workspace.ai_workspace.id
  tags                       = local.common_tags

  lifecycle {
    prevent_destroy = true
  }
}
