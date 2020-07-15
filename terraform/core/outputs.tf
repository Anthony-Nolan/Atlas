output "donor_import" {
  value = module.donor_import
}

output "match_prediction" {
  value = module.match_prediction
}

output "matching_algorithm" {
  value = module.matching_algorithm
}

output "multiple_allele_code_lookup" {
  value = module.multiple_allele_code_lookup
}

output "sql_server" {
  value = azurerm_sql_server.atlas_sql_server.name
}

output "storage_account" {
  value = {
    id = azurerm_storage_account.azure_storage.id
  }
}

output "matching-algorithm-persistent-db-name" {
  module.matching_algorithm.sql_database.persistent_database_name
}

output "matching-algorithm-persistent-db-conn-string" {
  module.matching_algorithm.sql_database.persistent_database_connection_string
}

output "matching-transient-a-name" {
  module.matching_algorithm.sql_database.transient_a_database_name
}

output "matching-algorithm-a-conn-string" {
  module.matching_algorithm.sql_database.transient_a_database_connection_string
}

output "matching-algorithm-b-name" {
  module.matching_algorithm.sql_database.transient_b_database_name
}

output "matching-algorithm-b-conn-string" {
  module.matching_algorithm.sql_database.transient_b_database_connection_string
}