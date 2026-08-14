locals {
  external_sql_fqdn = "${var.EXTERNAL_SQL_SERVER_NAME}.database.windows.net"

  // Single source of truth for WHICH server and databases the matching algorithm's transient data lives in.
  // Both the connection strings (data plane) and the Azure-management settings the data refresh scales
  // through (control plane - AzureManagement:Database:ServerName, DataRefresh:Database{A,B}Name) are derived
  // from these, so the two cannot disagree.
  //
  // They did disagree: the names were hardcoded to the Terraform-managed resources while only the connection
  // strings honoured USE_EXTERNAL_SQL. An external-SQL environment therefore scaled one database and wrote to
  // another, silently - both databases exist, so the scale call succeeds and nothing surfaces the mismatch.
  matching_sql_server_name = var.USE_EXTERNAL_SQL ? var.EXTERNAL_SQL_SERVER_NAME : var.sql_server.name
  matching_sql_server_fqdn = var.USE_EXTERNAL_SQL ? local.external_sql_fqdn : var.sql_server.fully_qualified_domain_name

  matching_transient_database_a_name = var.USE_EXTERNAL_SQL ? var.EXTERNAL_SQL_DB_MATCHING_A : azurerm_mssql_database.atlas-matching-transient-a.name
  matching_transient_database_b_name = var.USE_EXTERNAL_SQL ? var.EXTERNAL_SQL_DB_MATCHING_B : azurerm_mssql_database.atlas-matching-transient-b.name

  matching_transient_database_a_connection_string = "Server=tcp:${local.matching_sql_server_fqdn},1433;Initial Catalog=${local.matching_transient_database_a_name};Persist Security Info=False;User ID=${var.DATABASE_USERNAME};Password=${var.DATABASE_PASSWORD};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=${var.DATABASE_TRANSIENT_TIMEOUT};"
  matching_transient_database_b_connection_string = "Server=tcp:${local.matching_sql_server_fqdn},1433;Initial Catalog=${local.matching_transient_database_b_name};Persist Security Info=False;User ID=${var.DATABASE_USERNAME};Password=${var.DATABASE_PASSWORD};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=${var.DATABASE_TRANSIENT_TIMEOUT};"
  matching_persistent_database_connection_string = var.USE_EXTERNAL_SQL ? (
    "Server=tcp:${local.external_sql_fqdn},1433;Initial Catalog=${var.EXTERNAL_SQL_DB_SHARED};Persist Security Info=False;User ID=${var.DATABASE_USERNAME};Password=${var.DATABASE_PASSWORD};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=1800;"
    ) : (
    "Server=tcp:${var.sql_server.fully_qualified_domain_name},1433;Initial Catalog=${var.sql_database_shared.name};Persist Security Info=False;User ID=${var.DATABASE_USERNAME};Password=${var.DATABASE_PASSWORD};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=1800;"
  )
  matching_donor_database_connection_string = var.USE_EXTERNAL_SQL ? (
    "Server=tcp:${local.external_sql_fqdn},1433;Initial Catalog=${var.EXTERNAL_SQL_DB_SHARED};Persist Security Info=False;User ID=${var.DONOR_IMPORT_DATABASE_USERNAME};Password=${var.DONOR_IMPORT_DATABASE_PASSWORD};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=1800;"
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