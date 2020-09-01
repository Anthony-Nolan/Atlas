locals {
  matching_transient_database_a_connection_string = "Server=tcp:${var.sql_server.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_sql_database.atlas-matching-transient-a.name};Persist Security Info=False;User ID=${var.DATABASE_USERNAME};Password=${var.DATABASE_PASSWORD};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=${var.DATABASE_TRANSIENT_TIMEOUT};"
  matching_transient_database_b_connection_string = "Server=tcp:${var.sql_server.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_sql_database.atlas-matching-transient-b.name};Persist Security Info=False;User ID=${var.DATABASE_USERNAME};Password=${var.DATABASE_PASSWORD};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=${var.DATABASE_TRANSIENT_TIMEOUT};"
  matching_persistent_database_connection_string  = "Server=tcp:${var.sql_server.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_sql_database.atlas-persistent.name};Persist Security Info=False;User ID=${var.DATABASE_USERNAME};Password=${var.DATABASE_PASSWORD};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=1800;"
}

resource "azurerm_sql_database" "atlas-matching-transient-a" {
  name                = lower("${var.general.environment}-ATLAS-MATCHING-A")
  resource_group_name = var.app_service_plan.resource_group_name
  location            = var.general.location
  server_name         = var.sql_server.name
  tags                = var.general.common_tags
}

resource "azurerm_sql_database" "atlas-matching-transient-b" {
  name                = lower("${var.general.environment}-ATLAS-MATCHING-B")
  resource_group_name = var.app_service_plan.resource_group_name
  location            = var.general.location
  server_name         = var.sql_server.name
  tags                = var.general.common_tags
}

resource "azurerm_sql_database" "atlas-persistent" {
  name                = lower("${var.general.environment}-ATLAS-MATCHING-PERSISTENT")
  resource_group_name = var.app_service_plan.resource_group_name
  location            = var.general.location
  server_name         = var.sql_server.name
  tags                = var.general.common_tags
}

output "atlas-matching-transient-a-name" {
  value = lower("${var.general.environment}-ATLAS-MATCHING-A")
}

output "atlas-matching-transient-a-address" {
  value = var.sql_server.fully_qualified_domain_name
}
