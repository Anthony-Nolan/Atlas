resource "azurerm_sql_server" "atlas_sql_server" {
  name                         = lower("${local.environment}-ATLAS-SQL-SERVER")
  resource_group_name          = azurerm_resource_group.atlas_resource_group.name
  location                     = local.location
  tags                         = local.common_tags
  version                      = "12.0"
  administrator_login          = var.DATABASE_SERVER_ADMIN_LOGIN
  administrator_login_password = var.DATABASE_SERVER_ADMIN_LOGIN_PASSWORD
}

resource "azurerm_sql_firewall_rule" "firewall_rule_allow_azure" {
  name                = "AllowAzureServices"
  resource_group_name = azurerm_resource_group.atlas_resource_group.name
  server_name         = azurerm_sql_server.atlas_sql_server.name
  start_ip_address    = "0.0.0.0"
  end_ip_address      = "0.0.0.0"
}

resource "azurerm_sql_database" "atlas-database-shared" {
  location            = local.location
  name                = lower("${local.environment}-ATLAS")
  resource_group_name = azurerm_resource_group.atlas_resource_group.name
  server_name         = azurerm_sql_server.atlas_sql_server.name

  edition                          = "Standard"
  max_size_bytes                   = var.DATABASE_SHARED_MAX_SIZE
  requested_service_objective_name = var.DATABASE_SHARED_SKU_SIZE

  tags = local.common_tags
}
