locals {
  matching_transient_database_a_connection_string = "Server=tcp:${azurerm_sql_server.atlas_sql_server.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_sql_database.atlas-matching-transient-a.name};Persist Security Info=False;User ID=${var.MATCHING_DATABASE_USERNAME};Password=${var.MATCHING_DATABASE_PASSWORD};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=1800;"
  matching_transient_database_b_connection_string = "Server=tcp:${azurerm_sql_server.atlas_sql_server.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_sql_database.atlas-matching-transient-b.name};Persist Security Info=False;User ID=${var.MATCHING_DATABASE_USERNAME};Password=${var.MATCHING_DATABASE_PASSWORD};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=1800;"
  matching_persistent_database_connection_string  = "Server=tcp:${azurerm_sql_server.atlas_sql_server.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_sql_database.atlas-persistent.name};Persist Security Info=False;User ID=${var.MATCHING_DATABASE_USERNAME};Password=${var.MATCHING_DATABASE_PASSWORD};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=1800;"
  donor_import_connection_string = "Server=tcp:${azurerm_sql_server.atlas_sql_server.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_sql_database.atlas-donor-import.name};Persist Security Info=False;User ID=${var.ATLAS_MATCHING_DATABASE_USERNAME};Password=${var.ATLAS_MATCHING_DATABASE_PASSWORD};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=1800;"
}

resource "azurerm_sql_database" "atlas-matching-transient-a" {
  name                = lower("${local.environment}-ATLAS-MATCHING-A")
  resource_group_name = azurerm_resource_group.atlas_resource_group.name
  location            = local.location
  server_name         = azurerm_sql_server.atlas_sql_server.name
  tags                = local.common_tags
}

resource "azurerm_sql_database" "atlas-matching-transient-b" {
  name                = lower("${local.environment}-ATLAS-MATCHING-B")
  resource_group_name = azurerm_resource_group.atlas_resource_group.name
  location            = local.location
  server_name         = azurerm_sql_server.atlas_sql_server.name
  tags                = local.common_tags
}

resource "azurerm_sql_database" "atlas-persistent" {
  name                = lower("${local.environment}-ATLAS-MATCHING-PERSISTENT")
  resource_group_name = azurerm_resource_group.atlas_resource_group.name
  location            = local.location
  server_name         = azurerm_sql_server.atlas_sql_server.name
  tags                = local.common_tags
}
