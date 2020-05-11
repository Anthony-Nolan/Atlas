locals {
  donor_import_connection_string = "Server=tcp:${azurerm_sql_server.atlas_sql_server.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_sql_database.atlas-donor-import.name};Persist Security Info=False;User ID=${var.DONOR_DATABASE_USERNAME};Password=${var.DONOR_DATABASE_PASSWORD};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=1800;"
}

resource "azurerm_sql_database" "atlas-donor-import" {
  name                = lower("${local.environment}-ATLAS-DONORS")
  resource_group_name = azurerm_resource_group.atlas_resource_group.name
  location            = local.location
  server_name         = azurerm_sql_server.atlas_sql_server.name
  tags                = local.common_tags
}
