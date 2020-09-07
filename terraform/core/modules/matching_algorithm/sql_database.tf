locals {
  matching_transient_database_a_connection_string = "Server=tcp:${var.sql_server.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_sql_database.atlas-matching-transient-a.name};Persist Security Info=False;User ID=${var.DATABASE_USERNAME};Password=${var.DATABASE_PASSWORD};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=${var.DATABASE_TRANSIENT_TIMEOUT};"
  matching_transient_database_b_connection_string = "Server=tcp:${var.sql_server.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_sql_database.atlas-matching-transient-b.name};Persist Security Info=False;User ID=${var.DATABASE_USERNAME};Password=${var.DATABASE_PASSWORD};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=${var.DATABASE_TRANSIENT_TIMEOUT};"
  matching_persistent_database_connection_string  = "Server=tcp:${var.sql_server.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_sql_database.atlas-persistent.name};Persist Security Info=False;User ID=${var.DATABASE_USERNAME};Password=${var.DATABASE_PASSWORD};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=1800;"
}

resource "azurerm_sql_database" "atlas-matching-transient-a" {
  location            = var.general.location
  name                = lower("${var.general.environment}-ATLAS-MATCHING-A")
  resource_group_name = var.app_service_plan.resource_group_name
  server_name         = var.sql_server.name

  // DO NOT SET THE PRICING TIER IN TERRAFORM - this is dynamically scaled as part of the data refresh, and specifying any values in terraform will cause releases to override said scaling
  max_size_bytes = "268435456000"

  tags = var.general.common_tags
}

resource "azurerm_sql_database" "atlas-matching-transient-b" {
  location            = var.general.location
  name                = lower("${var.general.environment}-ATLAS-MATCHING-B")
  resource_group_name = var.app_service_plan.resource_group_name
  server_name         = var.sql_server.name

  // DO NOT SET THE PRICING TIER IN TERRAFORM - this is dynamically scaled as part of the data refresh, and specifying any values in terraform will cause releases to override said scaling
  max_size_bytes = "268435456000"

  tags = var.general.common_tags
}

resource "azurerm_sql_database" "atlas-persistent" {
  location            = var.general.location
  name                = lower("${var.general.environment}-ATLAS-MATCHING-PERSISTENT")
  resource_group_name = var.app_service_plan.resource_group_name
  server_name         = var.sql_server.name

  edition                          = "Standard"
  max_size_bytes                   = "32212254720"
  requested_service_objective_name = "S0"

  tags = var.general.common_tags
}