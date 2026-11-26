locals {
  external_sql_fqdn = "${var.EXTERNAL_SQL_SERVER_NAME}.database.windows.net"

  donor_import_connection_string = var.USE_EXTERNAL_SQL ? (
    "Server=tcp:${local.external_sql_fqdn},1433;Initial Catalog=${var.EXTERNAL_SQL_DB_SHARED};Persist Security Info=False;User ID=${var.DATABASE_USERNAME};Password=${var.DATABASE_PASSWORD};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=1800;"
    ) : (
    "Server=tcp:${var.sql_server.fully_qualified_domain_name},1433;Initial Catalog=${var.sql_database.name};Persist Security Info=False;User ID=${var.DATABASE_USERNAME};Password=${var.DATABASE_PASSWORD};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=1800;"
  )
}