// The outputs in this file are flat so that they can be read by azure Devops
// For outputs that are to be read by other terraform scripts, use outputs.tf

output "donor-database-username" {
  value = var.DONOR_DATABASE_USERNAME
}

output "donor-import-database-name" {
  value = module.donor_import.sql_database.name
}

output "donor-import-function-name" {
  value = module.donor_import.function_app.app_name
}

output "donor-matching-function-name" {
  value = module.matching_algorithm.function_app.donor_matching_app_name
}

output "function-app-name" {
  value = local.atlas_function_app_name
}

output "match-prediction-database-name" {
  value = module.match_prediction.sql_database.name
}

output "match-prediction-function-name" {
  value = module.match_prediction.function_app.app_name
}

output "match-prediction-database-username" {
  value = var.MATCH_PREDICTION_DATABASE_USERNAME
}

output "matching-algorithm-database-persistent-name" {
  value = module.matching_algorithm.sql_database.persistent_database_name
}

output "matching-algorithm-database-transient-a-name" {
  value = module.matching_algorithm.sql_database.transient_a_database_name
}

output "matching-algorithm-database-transient-b-name" {
  value = module.matching_algorithm.sql_database.transient_b_database_name
}

output "matching-algorithm-function-name" {
  value = module.matching_algorithm.function_app.app_name
}

output "matching-database-username" {
  value = var.MATCHING_DATABASE_USERNAME
}

output "matching-username-for-donor-import-database" {
  value = var.MATCHING_USERNAME_FOR_DONOR_IMPORT_DATABASE
}

output "public-api-function-app-name" {
  value = local.atlas_public_api_function_app_name
}

output "sql-server" {
  value = azurerm_sql_server.atlas_sql_server.fully_qualified_domain_name
}

output "sql-server-admin-login" {
  value = var.DATABASE_SERVER_ADMIN_LOGIN
}

output "sql-server-admin-login-password" {
  value = var.DATABASE_SERVER_ADMIN_LOGIN_PASSWORD
}
