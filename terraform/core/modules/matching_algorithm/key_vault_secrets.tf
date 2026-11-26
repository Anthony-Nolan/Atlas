locals {
  // SQL connection strings are owned by this module, so it declares them as secrets itself. The values are the same
  // locals the connection_string blocks used to hold inline, so USE_EXTERNAL_SQL is honoured either way.
  matching_key_vault_secrets = {
    "matching-transient-a-sql-connection-string" = local.matching_transient_database_a_connection_string
    "matching-transient-b-sql-connection-string" = local.matching_transient_database_b_connection_string
    "matching-persistent-sql-connection-string"  = local.matching_persistent_database_connection_string
    "matching-donor-sql-connection-string"       = local.matching_donor_database_connection_string
  }
}

// No role assignment needed: the shared identity holds "Key Vault Secrets User" over the whole vault, so it can read
// these as soon as they exist.
resource "azurerm_key_vault_secret" "matching_algorithm" {
  for_each = local.matching_key_vault_secrets

  name         = each.key
  value        = each.value
  key_vault_id = var.key_vault.id
  content_type = "connection-string"
  tags         = var.general.common_tags
}

locals {
  // Versioned SecretUri, matching the root module's choice - see the comment on local.kv_ref in
  // terraform/core/key_vault_secrets.tf.
  sql_kv_ref = {
    for name, secret in azurerm_key_vault_secret.matching_algorithm :
    name => "@Microsoft.KeyVault(SecretUri=${secret.id})"
  }
}
