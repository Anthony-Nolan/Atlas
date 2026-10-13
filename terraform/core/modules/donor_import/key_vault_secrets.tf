locals {
  // SQL connection strings are owned by this module, so it declares them as secrets itself. The value is the same local
  // the connection_string block used to hold inline, so USE_EXTERNAL_SQL is honoured either way.
  donor_import_key_vault_secrets = {
    "donor-import-sql-connection-string" = local.donor_import_connection_string
  }
}

// No role assignment needed: the shared identity holds "Key Vault Secrets User" over the whole vault, so it can read
// these as soon as they exist.
resource "azurerm_key_vault_secret" "donor_import" {
  for_each = local.donor_import_key_vault_secrets

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
    for name, secret in azurerm_key_vault_secret.donor_import :
    name => "@Microsoft.KeyVault(SecretUri=${secret.id})"
  }
}
