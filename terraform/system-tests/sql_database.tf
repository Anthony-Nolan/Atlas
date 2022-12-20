locals {
  donor_import_connection_string                  = "Server=tcp:${azurerm_mssql_server.atlas_sql_server.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_mssql_database.atlas-system-tests-shared.name};Persist Security Info=False;User ID=${var.DATABASE_SERVER_ADMIN_LOGIN};Password=${var.DATABASE_SERVER_ADMIN_LOGIN_PASSWORD};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=1800;"
  matching_algorithm_connection_string            = "Server=tcp:${azurerm_mssql_server.atlas_sql_server.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_mssql_database.atlas-matching.name};Persist Security Info=False;User ID=${var.DATABASE_SERVER_ADMIN_LOGIN};Password=${var.DATABASE_SERVER_ADMIN_LOGIN_PASSWORD};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=1800;"
  matching_algorithm_validation_connection_string = "Server=tcp:${azurerm_mssql_server.atlas_sql_server.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_mssql_database.atlas-matching-validation.name};Persist Security Info=False;User ID=${var.DATABASE_SERVER_ADMIN_LOGIN};Password=${var.DATABASE_SERVER_ADMIN_LOGIN_PASSWORD};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=1800;"
  match_prediction_connection_string              = "Server=tcp:${azurerm_mssql_server.atlas_sql_server.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_mssql_database.atlas-system-tests-shared.name};Persist Security Info=False;User ID=${var.DATABASE_SERVER_ADMIN_LOGIN};Password=${var.DATABASE_SERVER_ADMIN_LOGIN_PASSWORD};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=1800;"
}

resource "azurerm_mssql_database" "atlas-system-tests-shared" {
  name        = "atlas-test"
  server_id   = azurerm_mssql_server.atlas_sql_server.id
  max_size_gb = "30"
  sku_name    = "S1"
}

resource "azurerm_mssql_database" "atlas-matching" {
  name        = "atlas-test-matching-algorithm"
  server_id   = azurerm_mssql_server.atlas_sql_server.id
  max_size_gb = "30"
  sku_name    = "S0"

  tags = local.common_tags
}

resource "azurerm_mssql_database" "atlas-matching-validation" {
  name        = "atlas-test-matching-algorithm-validation"
  server_id   = azurerm_mssql_server.atlas_sql_server.id
  max_size_gb = "30"
  sku_name    = "S0"

  tags = local.common_tags
}
