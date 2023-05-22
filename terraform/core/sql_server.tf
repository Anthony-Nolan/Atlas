resource "azurerm_mssql_server" "atlas_sql_server" {
  name                         = lower("${local.environment}-ATLAS-SQL-SERVER")
  resource_group_name          = azurerm_resource_group.atlas_resource_group.name
  location                     = local.location
  minimum_tls_version          = "Disabled"
  tags                         = local.common_tags
  version                      = "12.0"
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
    ignore_changes  = [
      administrator_login_password,

      // Ignoring changes for this property, as terraform has inexplicably started to complain that
      // "`minimum_tls_version` cannot be removed once set, please set a valid value for this property"
      // even though the value of "Disabled" has never been changed.
      minimum_tls_version]
  }
}

resource "azurerm_mssql_firewall_rule" "firewall_rule_allow_azure" {
  name             = "AllowAzureServices"
  server_id        = azurerm_mssql_server.atlas_sql_server.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}

resource "azurerm_mssql_database" "atlas-database-shared" {
  name      = lower("${local.environment}-ATLAS")
  server_id = azurerm_mssql_server.atlas_sql_server.id

  max_size_gb = var.DATABASE_SHARED_MAX_SIZE_GB
  sku_name    = var.DATABASE_SHARED_SKU_SIZE

  tags = local.common_tags
}
