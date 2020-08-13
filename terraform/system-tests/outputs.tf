output "matching_algorithm_db_connection_string" {
  value = local.matching_algorithm_connection_string
}

output "matching_algorithm_validation_db_connection_string"{
  value = local.matching_algorithm_validation_connection_string
}

output "match_prediction_db_connection_string" {
  value = local.match_prediction_connection_string
}

output "donor_import_db_connection_string" {
  value = local.donor_import_connection_string
}