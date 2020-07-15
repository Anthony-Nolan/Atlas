// The outputs in this file are flat so that they can be read by azure
// For outputs that are to be read by other terraforms, use outputs.tf

output "matching-algorithm-persistent-db-name" {
  value = module.matching_algorithm.sql_database.persistent_database_name
}

output "matching-algorithm-persistent-db-conn-string" {
  value = module.matching_algorithm.sql_database.persistent_database_connection_string
}

output "matching-transient-a-name" {
  value = module.matching_algorithm.sql_database.transient_a_database_name
}

output "matching-algorithm-a-conn-string" {
  value = module.matching_algorithm.sql_database.transient_a_database_connection_string
}

output "matching-algorithm-b-name" {
  value = module.matching_algorithm.sql_database.transient_b_database_name
}

output "matching-algorithm-b-conn-string" {
  value = module.matching_algorithm.sql_database.transient_b_database_connection_string
}

output "sql-server" {
  value = module.matching_algorithm.sql_database.sql_server
}

output "matching-algorithm-function-name" {
  value = module.matching_algorithm.function_app.app_name
}

output "donor-matching-function-name" {
  value = module.matching_algorithm.function_app.donor_matching_app_name
}