resource "azurem_sql_server" "transient_a" {
  name = var.DATA_REFRESH_DATABASE_A_NAME
  resource_group_name = azurerm_resource_group.atlas_resource_group.name
  location = local.location
  tags = local.common.tags
  version = var.DATA_REFRESH_DATABASE_VERSION
  administrator_login = var.DATA_REFRESH_DATABASE_ADMIN_LOGIN
  administrator_login_password = var.DATA_REFRESH_DATABASE_ADMIN_PASSWORD
}

