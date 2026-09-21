// Shared Key Vault holding the secrets used by more than one Atlas component.
// Per-component secrets (SQL connection strings) live in the vault too, but are declared by the module that owns them.

data "azurerm_client_config" "current" {}

locals {
  // Key Vault names are globally unique and capped at 24 characters.
  // The longest environment is "LIVE-WMDA", giving "live-wmda-atlas-kv" (18).
  key_vault_name = lower("${local.environment}-atlas-kv")
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
}

// Allows an ops group to seed and rotate the secrets that are deliberately not managed by Terraform.
// See "Key Vault bootstrap" in README_Deployment.md.
resource "azurerm_role_assignment" "ops_kv_secrets_officer" {
  count                = var.KEY_VAULT_ADMINISTRATOR_OBJECTID != null && var.KEY_VAULT_ADMINISTRATOR_OBJECTID != "" ? 1 : 0
  scope                = azurerm_key_vault.atlas.id
  role_definition_name = "Key Vault Secrets Officer"
  principal_id         = var.KEY_VAULT_ADMINISTRATOR_OBJECTID
}

// Data-plane role assignments take up to a minute to propagate. Without this pause the first apply in a new
// environment fails with a 403 when it tries to write the secrets below.
resource "time_sleep" "wait_for_key_vault_rbac" {
  depends_on      = [azurerm_role_assignment.terraform_kv_secrets_officer]
  create_duration = "60s"
}
