// Hand-off from the shared Key Vault in the root module. `secret_refs` holds pre-built "@Microsoft.KeyVault(...)"
// strings keyed by secret name, for the secrets the root module owns. `secret_ids` holds the matching versioned secret
// IDs, for the container app's `key_vault_secret_id`, which does not accept a reference string.
variable "key_vault" {
  type = object({
    id                        = string
    function_apps_identity_id = string
    secret_refs               = map(string)
    secret_ids                = map(string)
  })
}
