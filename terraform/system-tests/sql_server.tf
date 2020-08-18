resource "azurerm_sql_server" "atlas_sql_server" {
  name                         = lower("ATLAS-SYSTEM-TEST-SQL-SERVER")
  resource_group_name          = azurerm_resource_group.atlas_system_tests_resource_group.name
  location                     = local.location
  tags                         = local.common_tags
  version                      = "12.0"
  administrator_login          = var.DATABASE_SERVER_ADMIN_LOGIN
  administrator_login_password = var.DATABASE_SERVER_ADMIN_LOGIN_PASSWORD
}

resource "azurerm_sql_firewall_rule" "firewall_rule_allow_azure" {
  name                = "AllowAzureServices"
  resource_group_name = azurerm_resource_group.atlas_system_tests_resource_group.name
  server_name         = azurerm_sql_server.atlas_sql_server.name
  start_ip_address    = "0.0.0.0"
  end_ip_address      = "0.0.0.0"
}