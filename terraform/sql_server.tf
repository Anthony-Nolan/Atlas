resource "azurem_sql_server" "atlas_sql_server" {
  name = "${local.environment}-ATLAS-SQL-SERVER"
  resource_group_name = azurerm_resource_group.atlas_resource_group.name
  location = local.location
  tags = local.common.tags
  version = "12.0"
  administrator_login = var.ATLAS_DATABASE_SERVER_LOGIN
}
