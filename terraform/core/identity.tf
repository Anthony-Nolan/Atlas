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
