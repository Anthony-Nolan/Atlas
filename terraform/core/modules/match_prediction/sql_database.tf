locals {
  match_prediction_database_connection_string = "Server=tcp:${var.sql_server.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_sql_database.atlas-match-prediction.name};Persist Security Info=False;User ID=${var.DATABASE_USERNAME};Password=${var.DATABASE_PASSWORD};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=1800;"
}

resource "azurerm_sql_database" "atlas-match-prediction" {
  location            = var.general.location
  name                = lower("${var.general.environment}-ATLAS-MATCH-PREDICTION")
  resource_group_name = var.app_service_plan.resource_group_name
  server_name         = var.sql_server.name

  edition                          = "Standard"
  max_size_bytes                   = var.DATABASE_MAX_SIZE
  requested_service_objective_name = var.DATABASE_SKU_SIZE

  tags = var.general.common_tags
}
