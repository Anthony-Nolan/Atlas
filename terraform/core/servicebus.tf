locals {
  service-bus = {
    default-sku                    = "Standard" // Required for Topics.
    long-expiry                    = "P9999D"   // 2.75 years
    audit-subscription-idle-delete = "P8D"
    default-read-lock              = "PT5M"
    default-bus-size               = 5120 // 5GB
    default-message-retries        = 10
  }
}

resource "azurerm_servicebus_namespace" "general" {
  name                = "${lower(local.environment)}-atlas"
  location            = azurerm_resource_group.atlas_resource_group.location
  resource_group_name = azurerm_resource_group.atlas_resource_group.name
  sku                 = local.service-bus.default-sku
}

resource "azurerm_servicebus_namespace_authorization_rule" "read-only" {
  name                = "read-only"
  namespace_name      = azurerm_servicebus_namespace.general.name
  resource_group_name = azurerm_resource_group.atlas_resource_group.name

  listen = true
  send   = false
  manage = false
}

resource "azurerm_servicebus_namespace_authorization_rule" "write-only" {
  name                = "write-only"
  namespace_name      = azurerm_servicebus_namespace.general.name
  resource_group_name = azurerm_resource_group.atlas_resource_group.name

  listen = false
  send   = true
  manage = false
}

resource "azurerm_servicebus_namespace_authorization_rule" "read-write" {
  name                = "read-write"
  namespace_name      = azurerm_servicebus_namespace.general.name
  resource_group_name = azurerm_resource_group.atlas_resource_group.name

  listen = true
  send   = true
  manage = false
}