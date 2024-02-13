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
  sku                 = var.LOG_ANALYTICS_SKU
  daily_quota_gb      = var.LOG_ANALYTICS_DAILY_QUOTA_GB
  retention_in_days   = 90
}