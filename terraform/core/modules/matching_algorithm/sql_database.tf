locals {
  matching_transient_database_a_connection_string = "Server=tcp:${var.sql_server.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_sql_database.atlas-matching-transient-a.name};Persist Security Info=False;User ID=${var.DATABASE_USERNAME};Password=${var.DATABASE_PASSWORD};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=${var.DATABASE_TRANSIENT_TIMEOUT};"
  matching_transient_database_b_connection_string = "Server=tcp:${var.sql_server.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_sql_database.atlas-matching-transient-b.name};Persist Security Info=False;User ID=${var.DATABASE_USERNAME};Password=${var.DATABASE_PASSWORD};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=${var.DATABASE_TRANSIENT_TIMEOUT};"
  matching_persistent_database_connection_string  = "Server=tcp:${var.sql_server.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_sql_database.atlas-persistent.name};Persist Security Info=False;User ID=${var.DATABASE_USERNAME};Password=${var.DATABASE_PASSWORD};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=1800;"
}

resource "azurerm_sql_database" "atlas-matching-transient-a" {
  edition                          = "Standard"
  location                         = var.general.location
  max_size_bytes                   = "32212254720"
  name                             = lower("${var.general.environment}-ATLAS-MATCHING-A")
  requested_service_objective_name = "S0"
  resource_group_name              = var.app_service_plan.resource_group_name
  server_name                      = var.sql_server.name
  tags                             = var.general.common_tags
}

resource "azurerm_sql_database" "atlas-matching-transient-b" {
  edition                          = "Standard"
  location                         = var.general.location
  max_size_bytes                   = "268435456000"
  name                             = lower("${var.general.environment}-ATLAS-MATCHING-B")
  requested_service_objective_name = "S0"
  resource_group_name              = var.app_service_plan.resource_group_name
  server_name                      = var.sql_server.name
  tags                             = var.general.common_tags
}

resource "azurerm_sql_database" "atlas-persistent" {
  edition                          = "Standard"
  location                         = var.general.location
  max_size_bytes                   = "268435456000"
  name                             = lower("${var.general.environment}-ATLAS-MATCHING-PERSISTENT")
  requested_service_objective_name = "S0"
  resource_group_name              = var.app_service_plan.resource_group_name
  server_name                      = var.sql_server.name
  tags                             = var.general.common_tags
}

output "atlas-matching-transient-a-name" {
  value = lower("${var.general.environment}-ATLAS-MATCHING-A")
}

output "atlas-matching-transient-a-address" {
  value = var.sql_server.fully_qualified_domain_name
}
