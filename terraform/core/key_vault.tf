// Shared Key Vault holding the secrets used by more than one Atlas component.
// Per-component secrets (SQL connection strings) live in the vault too, but are declared by the module that owns them.

data "azurerm_client_config" "current" {}

locals {
  // Key Vault names are globally unique across ALL of Azure, not just this tenant, and capped at 24 characters.
  // "<env>-atlas-kv" collided with a vault in an unrelated tenant, so names carry an "an-" (Anthony Nolan) prefix.
  // The longest environment is "LIVE-WMDA", giving "an-live-wmda-atlas-kv" (21).
  key_vault_name = lower("an-${local.environment}-atlas-kv")
}

resource "azurerm_key_vault" "atlas" {
  name                       = local.key_vault_name
  resource_group_name        = azurerm_resource_group.atlas_resource_group.name
  location                   = local.location
  tenant_id                  = data.azurerm_client_config.current.tenant_id
  sku_name                   = "standard"
  rbac_authorization_enabled = true
  purge_protection_enabled   = var.KEY_VAULT_PURGE_PROTECTION_ENABLED
  soft_delete_retention_days = var.KEY_VAULT_SOFT_DELETE_RETENTION_DAYS
  tags                       = local.common_tags

  lifecycle {
    // Deleting the vault would break every function app at once, and a soft-deleted vault blocks re-use of the name.
    prevent_destroy = true
  }
}

// Identity shared by all function apps, used solely to resolve their @Microsoft.KeyVault app settings.
// Deliberately separate from azurerm_user_assigned_identity.aca_identity, which exists for ACR pull and
// Service Bus data-plane receive on the match prediction container app and should not gain secret-read rights.
resource "azurerm_user_assigned_identity" "function_apps_identity" {
  name                = lower("${local.environment}-ATLAS-ID-FUNCTIONS")
  resource_group_name = azurerm_resource_group.atlas_resource_group.name
  location            = local.location
  tags                = local.common_tags
}

resource "azurerm_role_assignment" "function_apps_identity_kv_secrets_user" {
  scope                = azurerm_key_vault.atlas.id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = azurerm_user_assigned_identity.function_apps_identity.principal_id
}

// With RBAC authorization the principal that creates the vault gets no data-plane access by default,
// so Terraform cannot manage azurerm_key_vault_secret resources without granting itself this role.
resource "azurerm_role_assignment" "terraform_kv_secrets_officer" {
  scope                = azurerm_key_vault.atlas.id
  role_definition_name = "Key Vault Secrets Officer"
  principal_id         = data.azurerm_client_config.current.object_id

  lifecycle {
    // This resolves to whoever is running Terraform, so without this a plan run by a human shows it being replaced,
    // and an apply would move the role off the deploy service principal and break the next pipeline run. The grant is
    // only ever needed by the principal that created it, so pinning it to that principal is the correct behaviour.
    // Grant anyone else through KEY_VAULT_SECRETS_OFFICER_OBJECTIDS instead.
    ignore_changes = [principal_id]
  }
}

// Allows named principals - typically an ops AD group - to seed and rotate the secrets that are deliberately not
// managed by Terraform. See "Key Vault bootstrap" in README_Deployment.md.
resource "azurerm_role_assignment" "kv_secrets_officers" {
  for_each = toset(var.KEY_VAULT_SECRETS_OFFICER_OBJECTIDS)

  scope                = azurerm_key_vault.atlas.id
  role_definition_name = "Key Vault Secrets Officer"
  principal_id         = each.value
}

// Data-plane role assignments take up to a minute to propagate. Without this pause the first apply in a new
// environment fails with a 403 when it tries to write the secrets below.
resource "time_sleep" "wait_for_key_vault_rbac" {
  depends_on      = [azurerm_role_assignment.terraform_kv_secrets_officer]
  create_duration = "60s"
}
