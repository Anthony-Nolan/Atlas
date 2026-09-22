// Hand-off from the shared Key Vault in the root module. `secret_refs` holds pre-built "@Microsoft.KeyVault(...)"
// strings keyed by secret name, for the secrets the root module owns.
variable "key_vault" {
  type = object({
    id                        = string
    function_apps_identity_id = string
    secret_refs               = map(string)
  })
}
