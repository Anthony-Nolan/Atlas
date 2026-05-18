locals {
  // Temporary escape hatch for environments such as DEV perf testing.
  // Remove DATABASE_SERVER_EXISTING_* values to revert to the standard managed Atlas SQL server.
  use_existing_sql_server             = var.DATABASE_SERVER_EXISTING_NAME != null
  existing_sql_server_resource_group  = coalesce(var.DATABASE_SERVER_EXISTING_RESOURCE_GROUP_NAME, local.resource_group_name)
  existing_sql_server_admin_login     = coalesce(var.DATABASE_SERVER_EXISTING_ADMIN_LOGIN, var.DATABASE_SERVER_ADMIN_LOGIN)
  atlas_sql_server = local.use_existing_sql_server ? {
    id                          = data.azurerm_mssql_server.atlas_sql_server[0].id
    name                        = data.azurerm_mssql_server.atlas_sql_server[0].name
    fully_qualified_domain_name = data.azurerm_mssql_server.atlas_sql_server[0].fully_qualified_domain_name
  } : {
    id                          = azurerm_mssql_server.atlas_sql_server[0].id
    name                        = azurerm_mssql_server.atlas_sql_server[0].name
    fully_qualified_domain_name = azurerm_mssql_server.atlas_sql_server[0].fully_qualified_domain_name
  }
}

data "azurerm_mssql_server" "atlas_sql_server" {
  count               = local.use_existing_sql_server ? 1 : 0
  name                = var.DATABASE_SERVER_EXISTING_NAME
  resource_group_name = local.existing_sql_server_resource_group
}

resource "azurerm_mssql_server" "atlas_sql_server" {
  count                        = local.use_existing_sql_server ? 0 : 1
  name                         = lower("${local.environment}-ATLAS-SQL-SERVER")
  resource_group_name          = azurerm_resource_group.atlas_resource_group.name
  location                     = local.location
  minimum_tls_version          = "1.2"
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
    ignore_changes = [
      administrator_login_password
    ]
  }
}

resource "azurerm_mssql_firewall_rule" "firewall_rule_allow_azure" {
  // Existing servers may already be configured outside this state, so avoid importing firewall rules implicitly.
  count            = local.use_existing_sql_server ? 0 : 1
  name             = "AllowAzureServices"
  server_id        = local.atlas_sql_server.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}

resource "azurerm_mssql_database" "atlas-database-shared" {
  name      = lower("${local.environment}-ATLAS")
  server_id = local.atlas_sql_server.id

  max_size_gb = var.DATABASE_SHARED_MAX_SIZE_GB
  sku_name    = var.DATABASE_SHARED_SKU_SIZE

  tags = local.common_tags
}
