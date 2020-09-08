locals {
  donor_import_connection_string                  = "Server=tcp:${azurerm_sql_server.atlas_sql_server.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_sql_database.atlas-donor-import.name};Persist Security Info=False;User ID=${var.DATABASE_SERVER_ADMIN_LOGIN};Password=${var.DATABASE_SERVER_ADMIN_LOGIN_PASSWORD};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=1800;"
  matching_algorithm_connection_string            = "Server=tcp:${azurerm_sql_server.atlas_sql_server.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_sql_database.atlas-matching.name};Persist Security Info=False;User ID=${var.DATABASE_SERVER_ADMIN_LOGIN};Password=${var.DATABASE_SERVER_ADMIN_LOGIN_PASSWORD};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=1800;"
  matching_algorithm_validation_connection_string = "Server=tcp:${azurerm_sql_server.atlas_sql_server.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_sql_database.atlas-matching-validation.name};Persist Security Info=False;User ID=${var.DATABASE_SERVER_ADMIN_LOGIN};Password=${var.DATABASE_SERVER_ADMIN_LOGIN_PASSWORD};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=1800;"
  match_prediction_connection_string              = "Server=tcp:${azurerm_sql_server.atlas_sql_server.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_sql_database.atlas-match-prediction.name};Persist Security Info=False;User ID=${var.DATABASE_SERVER_ADMIN_LOGIN};Password=${var.DATABASE_SERVER_ADMIN_LOGIN_PASSWORD};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=1800;"
}

resource "azurerm_sql_database" "atlas-donor-import" {
  location            = local.location
  name                = "atlas-test-donor-import"
  resource_group_name = azurerm_resource_group.atlas_system_tests_resource_group.name
  server_name         = azurerm_sql_server.atlas_sql_server.name

  edition                          = "Standard"
  requested_service_objective_name = "S0"

  tags = local.common_tags
}

resource "azurerm_sql_database" "atlas-matching" {
  location            = local.location
  name                = "atlas-test-matching-algorithm"
  resource_group_name = azurerm_resource_group.atlas_system_tests_resource_group.name
  server_name         = azurerm_sql_server.atlas_sql_server.name

  edition                          = "Standard"
  requested_service_objective_name = "S0"

  tags = local.common_tags
}

resource "azurerm_sql_database" "atlas-match-prediction" {
  location            = local.location
  name                = "atlas-test-match-prediction"
  resource_group_name = azurerm_resource_group.atlas_system_tests_resource_group.name
  server_name         = azurerm_sql_server.atlas_sql_server.name

  edition                          = "Standard"
  requested_service_objective_name = "S0"

  tags = local.common_tags
}

resource "azurerm_sql_database" "atlas-matching-validation" {
  location            = local.location
  name                = "atlas-test-matching-algorithm-validation"
  resource_group_name = azurerm_resource_group.atlas_system_tests_resource_group.name
  server_name         = azurerm_sql_server.atlas_sql_server.name

  edition                          = "Standard"
  requested_service_objective_name = "S0"

  tags = local.common_tags
}
