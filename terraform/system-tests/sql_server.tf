resource "azurerm_mssql_server" "atlas_sql_server" {
  name                         = lower("ATLAS-SYSTEM-TEST-SQL-SERVER")
  resource_group_name          = azurerm_resource_group.atlas_system_tests_resource_group.name
  location                     = local.location
  tags                         = local.common_tags
  version                      = "12.0"
  minimum_tls_version          = "Disabled"
  administrator_login          = var.DATABASE_SERVER_ADMIN_LOGIN
  administrator_login_password = var.DATABASE_SERVER_ADMIN_LOGIN_PASSWORD

  azuread_administrator {
    azuread_authentication_only = false
    login_username              = var.DATABASE_SERVER_AZUREAD_ADMINISTRATOR_LOGIN_USERNAME
    object_id                   = var.DATABASE_SERVER_AZUREAD_ADMINISTRATOR_OBJECTID
    tenant_id                   = var.DATABASE_SERVER_AZUREAD_ADMINISTRATOR_TENANTID
  }

    lifecycle {
    prevent_destroy = true
    ignore_changes  = [administrator_login_password]
  }

}

resource "azurerm_mssql_firewall_rule" "firewall_rule_allow_azure" {
  name             = "AllowAzureServices"
  server_id        = azurerm_mssql_server.atlas_sql_server.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}