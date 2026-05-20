locals {
  external_sql_fqdn = "${var.external_sql_server_name}.database.windows.net"

  matching_transient_database_a_connection_string = var.use_external_sql ? (
    "Server=tcp:${local.external_sql_fqdn},1433;Initial Catalog=${var.external_sql_db_matching_a};Persist Security Info=False;User ID=${var.DATABASE_USERNAME};Password=${var.DATABASE_PASSWORD};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=${var.DATABASE_TRANSIENT_TIMEOUT};"
    ) : (
    "Server=tcp:${var.sql_server.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_mssql_database.atlas-matching-transient-a.name};Persist Security Info=False;User ID=${var.DATABASE_USERNAME};Password=${var.DATABASE_PASSWORD};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=${var.DATABASE_TRANSIENT_TIMEOUT};"
  )
  matching_transient_database_b_connection_string = var.use_external_sql ? (
    "Server=tcp:${local.external_sql_fqdn},1433;Initial Catalog=${var.external_sql_db_matching_b};Persist Security Info=False;User ID=${var.DATABASE_USERNAME};Password=${var.DATABASE_PASSWORD};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=${var.DATABASE_TRANSIENT_TIMEOUT};"
    ) : (
    "Server=tcp:${var.sql_server.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_mssql_database.atlas-matching-transient-b.name};Persist Security Info=False;User ID=${var.DATABASE_USERNAME};Password=${var.DATABASE_PASSWORD};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=${var.DATABASE_TRANSIENT_TIMEOUT};"
  )
  matching_persistent_database_connection_string = var.use_external_sql ? (
    "Server=tcp:${local.external_sql_fqdn},1433;Initial Catalog=${var.external_sql_db_shared};Persist Security Info=False;User ID=${var.DATABASE_USERNAME};Password=${var.DATABASE_PASSWORD};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=1800;"
    ) : (
    "Server=tcp:${var.sql_server.fully_qualified_domain_name},1433;Initial Catalog=${var.sql_database_shared.name};Persist Security Info=False;User ID=${var.DATABASE_USERNAME};Password=${var.DATABASE_PASSWORD};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=1800;"
  )
  matching_donor_database_connection_string = var.use_external_sql ? (
    "Server=tcp:${local.external_sql_fqdn},1433;Initial Catalog=${var.external_sql_db_shared};Persist Security Info=False;User ID=${var.DONOR_IMPORT_DATABASE_USERNAME};Password=${var.DONOR_IMPORT_DATABASE_PASSWORD};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=1800;"
    ) : (
    "Server=tcp:${var.sql_server.fully_qualified_domain_name},1433;Initial Catalog=${var.donor_import_sql_database.name};Persist Security Info=False;User ID=${var.DONOR_IMPORT_DATABASE_USERNAME};Password=${var.DONOR_IMPORT_DATABASE_PASSWORD};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=1800;"
  )
}

resource "azurerm_mssql_database" "atlas-matching-transient-a" {
  name      = lower("${var.general.environment}-ATLAS-MATCHING-A")
  server_id = var.sql_server.id

  max_size_gb = var.DATABASE_MAX_SIZE_GB

  lifecycle {
    ignore_changes = [
      // DO NOT SET THE PRICING TIER IN TERRAFORM - this is dynamically scaled as part of the data refresh, and specifying any values in terraform will cause releases to override said scaling
      sku_name
    ]
  }

  tags = var.general.common_tags
}

resource "azurerm_mssql_database" "atlas-matching-transient-b" {
  name      = lower("${var.general.environment}-ATLAS-MATCHING-B")
  server_id = var.sql_server.id

  max_size_gb = var.DATABASE_MAX_SIZE_GB

  lifecycle {
    ignore_changes = [
      // DO NOT SET THE PRICING TIER IN TERRAFORM - this is dynamically scaled as part of the data refresh, and specifying any values in terraform will cause releases to override said scaling
      sku_name
    ]
  }

  tags = var.general.common_tags
}