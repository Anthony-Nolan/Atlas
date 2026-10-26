// Identity shared by the Atlas apps - the function apps and the container apps - for Microsoft Entra ID access to other
// resources: the Key Vault (key_vault.tf) and the distributed cache (distributed_cache.tf). Grant it access where the
// resource is declared, and pass it to the component modules as their function_apps_identity variable, not as part of
// another resource's hand-off.
// Deliberately separate from aca_identity below, which exists for ACR pull and Service Bus data-plane receive on the
// container apps and should not gain secret-read rights.
resource "azurerm_user_assigned_identity" "function_apps_identity" {
  name                = lower("${local.environment}-ATLAS-ID-FUNCTIONS")
  resource_group_name = azurerm_resource_group.atlas_resource_group.name
  location            = local.location
  tags                = local.common_tags
}

resource "azurerm_user_assigned_identity" "aca_identity" {
  name                = lower("${local.environment}-ATLAS-ID-ACA")
  resource_group_name = azurerm_resource_group.atlas_resource_group.name
  location            = local.location
  tags                = local.common_tags
}

resource "azurerm_role_assignment" "aca_identity_acr_pull" {
  scope                = data.azurerm_container_registry.shared.id
  role_definition_name = "AcrPull"
  principal_id         = azurerm_user_assigned_identity.aca_identity.principal_id
}

resource "azurerm_role_assignment" "aca_identity_servicebus_data_receiver" {
  scope                = azurerm_servicebus_namespace.general.id
  role_definition_name = "Azure Service Bus Data Receiver"
  principal_id         = azurerm_user_assigned_identity.aca_identity.principal_id
}
