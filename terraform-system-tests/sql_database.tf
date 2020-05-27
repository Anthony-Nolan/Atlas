locals {
  donor_import_connection_string = "Server=tcp:${azurerm_sql_server.atlas_sql_server.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_sql_database.atlas-donor-import.name};Persist Security Info=False;User ID=${var.DATABASE_SERVER_ADMIN_LOGIN};Password=${var.DATABASE_SERVER_ADMIN_LOGIN_PASSWORD};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=1800;"
}

resource "azurerm_sql_database" "atlas-donor-import" {
  name                = lower("ATLAS-TEST-DONOR-IMPORT")
  resource_group_name = azurerm_resource_group.atlas_resource_group.name
  location            = local.location
  server_name         = azurerm_sql_server.atlas_sql_server.name
  tags                = local.common_tags
}

resource "azurerm_sql_database" "atlas-matching" {
  name                = lower("ATLAS-TEST-MATCHING-ALGORITHM")
  resource_group_name = azurerm_resource_group.atlas_resource_group.name
  location            = local.location
  server_name         = azurerm_sql_server.atlas_sql_server.name
  tags                = local.common_tags
}

resource "azurerm_sql_database" "atlas-match-prediction" {
  name                = lower("ATLAS-TEST-MATCH-PREDICTION")
  resource_group_name = azurerm_resource_group.atlas_resource_group.name
  location            = local.location
  server_name         = azurerm_sql_server.atlas_sql_server.name
  tags                = local.common_tags
}
