resource "azurerm_sql_server" "atlas_sql_server" {
  name = lower("${local.environment}-ATLAS-SQL-SERVER")
  resource_group_name = azurerm_resource_group.atlas_resource_group.name
  location = local.location
  tags = local.common_tags
  version = "12.0"
  administrator_login = var.ATLAS_DATABASE_SERVER_ADMIN_LOGIN
  administrator_login_password = var.ATLAS_DATABASE_SERVER_LOGIN_PASSWORD
}
