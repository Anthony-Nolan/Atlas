locals {
  // SQL connection strings are owned by this module, so it declares them as secrets itself. The value is the same local
  // the connection_string block used to hold inline, so USE_EXTERNAL_SQL is honoured either way.
  repeat_search_key_vault_secrets = {
    "repeat-search-sql-connection-string" = local.repeat_search_database_connection_string
  }
}

// No role assignment needed: the shared identity holds "Key Vault Secrets User" over the whole vault, so it can read
// these as soon as they exist.
resource "azurerm_key_vault_secret" "repeat_search" {
  for_each = local.repeat_search_key_vault_secrets

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
    for name, secret in azurerm_key_vault_secret.repeat_search :
    name => "@Microsoft.KeyVault(SecretUri=${secret.id})"
  }
}
