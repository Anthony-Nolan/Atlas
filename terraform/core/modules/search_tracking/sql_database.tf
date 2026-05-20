locals {
  external_sql_fqdn = "${var.external_sql_server_name}.database.windows.net"

  search_tracking_database_connection_string = var.use_external_sql ? (
    "Server=tcp:${local.external_sql_fqdn},1433;Initial Catalog=${var.external_sql_db_shared};Persist Security Info=False;User ID=${var.DATABASE_USERNAME};Password=${var.DATABASE_PASSWORD};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=1800;"
    ) : (
    "Server=tcp:${var.sql_server.fully_qualified_domain_name},1433;Initial Catalog=${var.sql_database.name};Persist Security Info=False;User ID=${var.DATABASE_USERNAME};Password=${var.DATABASE_PASSWORD};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=1800;"
  )
}