locals {
  // Secrets that are seeded into the Atlas Key Vault by hand rather than by Terraform - see "Key Vault bootstrap" in
  // README_Deployment.md. The apps reference the first two directly. The rest are read at the start of each release by
  // the Key Vault-linked ADO variable group, which hands them to Terraform as TF_VAR_* and to the database user and
  // migration steps - see "Release variable group" in README_Deployment.md.
  hand_seeded_key_vault_secret_names = [
    "azure-client-secret",
    "automapper-license-key",
    "database-server-admin-login-password",
    "donor-database-password",
    "matching-database-password",
    "matching-password-for-donor-import-database",
    "match-prediction-database-password",
    "repeat-search-database-password",
    "search-tracking-database-password",
  ]
}

// Fails the plan, naming the secret and the vault, if any hand-seeded secret is missing or disabled. Without this, a
// missing secret only shows up at runtime, as an app setting that resolves to the literal "@Microsoft.KeyVault(...)"
// string. Ephemeral, so the values are read but never written to state or to the plan file.
ephemeral "azurerm_key_vault_secret" "hand_seeded" {
  for_each = toset(local.hand_seeded_key_vault_secret_names)

  name         = each.value
  key_vault_id = azurerm_key_vault.atlas.id

  depends_on = [time_sleep.wait_for_key_vault_rbac]
}
