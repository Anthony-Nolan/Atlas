locals {
  // Secrets Terraform owns, because it owns the resources they are derived from. Their values are already held in
  // state via those resources, so storing them in the vault adds no new state exposure - it removes them from the
  // function apps' configuration, which is the point.
  shared_key_vault_secrets = {
    "azure-storage-connection-string"         = azurerm_storage_account.azure_storage.primary_connection_string
    "servicebus-read-write-connection-string" = azurerm_servicebus_namespace_authorization_rule.read-write.primary_connection_string
    "servicebus-read-only-connection-string"  = azurerm_servicebus_namespace_authorization_rule.read-only.primary_connection_string
    "servicebus-write-only-connection-string" = azurerm_servicebus_namespace_authorization_rule.write-only.primary_connection_string
  }
}

resource "azurerm_key_vault_secret" "shared" {
  for_each = local.shared_key_vault_secrets

  name         = each.key
  value        = each.value
  key_vault_id = azurerm_key_vault.atlas.id
  content_type = "connection-string"
  tags         = local.common_tags

  depends_on = [time_sleep.wait_for_key_vault_rbac]
}

locals {
  // Versioned SecretUri. When Terraform rotates a value it writes a new secret version and updates the app setting in
  // the same apply, so the app restarts and picks the new value up immediately rather than waiting out the platform's
  // 24 hour reference cache.
  kv_ref = {
    for name, secret in azurerm_key_vault_secret.shared :
    name => "@Microsoft.KeyVault(SecretUri=${secret.id})"
  }

  // Seeded outside Terraform - see "Key Vault bootstrap" in README_Deployment.md. Referenced by vault and secret name
  // so the value never enters Terraform state; a data "azurerm_key_vault_secret" would read it into state and defeat
  // the purpose. Consumed by the matching algorithm module, not by the root function apps.
  azure_client_secret_kv_ref = "@Microsoft.KeyVault(VaultName=${azurerm_key_vault.atlas.name};SecretName=azure-client-secret)"
}
