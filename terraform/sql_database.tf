resource "azurerm_sql_database" "atlas-data-refresh-a" {
  name = lower("${local.environment}-ATLAS-MATCHING-A")
  resource_group_name = azurerm_resource_group.atlas_resource_group.name
  location = local.location
  server_name = azurerm_sql_server.atlas_sql_server.name
  tags = local.common_tags
}

resource "azurerm_sql_database" "atlas-data-refresh-b" {
  name = lower("${local.environment}-ATLAS-MATCHING-B")
  resource_group_name = azurerm_resource_group.atlas_resource_group.name
  location = local.location
  server_name = azurerm_sql_server.atlas_sql_server.name
  tags = local.common_tags
}

resource "azurerm_sql_database" "atlas-persistent" {
  name = lower("${local.environment}-ATLAS-MATCHING-PERSISTENT")
  resource_group_name = azurerm_resource_group.atlas_resource_group.name
  location = local.location
  server_name = azurerm_sql_server.atlas_sql_server.name
  tags = local.common_tags
}