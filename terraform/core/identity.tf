resource "azurerm_user_assigned_identity" "acr_pull" {
  name                = lower("${local.environment}-ATLAS-ID-ACR-PULL")
  resource_group_name = azurerm_resource_group.atlas_resource_group.name
  location            = local.location
  tags                = local.common_tags
}

resource "azurerm_role_assignment" "acr_pull" {
  scope                = data.azurerm_container_registry.shared.id
  role_definition_name = "AcrPull"
  principal_id         = azurerm_user_assigned_identity.acr_pull.principal_id
}
