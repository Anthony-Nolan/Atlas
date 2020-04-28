locals {
  service-bus = {
    default-sku                    = "Standard"
    long-expiry                    = "P9999D" // 2.75 years
    audit-subscription-idle-delete = "P8D"
    default-read-lock              = "PT5M"
    default-bus-size               = 5120 // 5GB
    default-message-retries        = 10
  }
}

// Three ServiceBus "Namespaces". All identical except name.
// "Standard Tier" is required for Topics.
resource "azurerm_servicebus_namespace" "general" {
  name                = "${local.environment}-atlas-general"
  location            = azurerm_resource_group.atlas_resource_group.location
  resource_group_name = azurerm_resource_group.atlas_resource_group.name
  sku                 = local.service-bus.default-sku
}

resource "azurerm_servicebus_namespace" "notifications" {
  name                = "${local.environment}-atlas-notifications"
  location            = azurerm_resource_group.atlas_resource_group.location
  resource_group_name = azurerm_resource_group.atlas_resource_group.name
  sku                 = local.service-bus.default-sku
}

// Topics for Notifications, Results, and Donor Updates; Queue for Searches.
// Identical except for:
//   enable_express for Searches.
//   support_ordering for donor updates.

resource "azurerm_servicebus_topic" "notifications" {
  name                  = "notifications"
  resource_group_name   = azurerm_resource_group.atlas_resource_group.name
  namespace_name        = azurerm_servicebus_namespace.notifications.name
  auto_delete_on_idle   = local.service-bus.long-expiry
  default_message_ttl   = local.service-bus.long-expiry
  max_size_in_megabytes = local.service-bus.default-bus-size
}

resource "azurerm_servicebus_topic" "alerts" {
  name                  = "alerts"
  resource_group_name   = azurerm_resource_group.atlas_resource_group.name
  namespace_name        = azurerm_servicebus_namespace.notifications.name
  auto_delete_on_idle   = local.service-bus.long-expiry
  default_message_ttl   = local.service-bus.long-expiry
  max_size_in_megabytes = local.service-bus.default-bus-size
}

resource "azurerm_servicebus_topic" "matching-results-ready" {
  name                  = "matching-results-ready"
  resource_group_name   = azurerm_resource_group.atlas_resource_group.name
  namespace_name        = azurerm_servicebus_namespace.general.name
  auto_delete_on_idle   = local.service-bus.long-expiry
  default_message_ttl   = local.service-bus.long-expiry
  max_size_in_megabytes = local.service-bus.default-bus-size
  support_ordering      = true
}

resource "azurerm_servicebus_topic" "updated-searchable-donors" {
  name                  = "updated-searchable-donors"
  resource_group_name   = azurerm_resource_group.atlas_resource_group.name
  namespace_name        = azurerm_servicebus_namespace.general.name
  auto_delete_on_idle   = local.service-bus.long-expiry
  default_message_ttl   = local.service-bus.long-expiry
  max_size_in_megabytes = local.service-bus.default-bus-size
  support_ordering      = true
}

resource "azurerm_servicebus_subscription" "donor-update" {
  name                                 = "donor-update"
  resource_group_name                  = azurerm_resource_group.atlas_resource_group.name
  namespace_name                       = azurerm_servicebus_namespace.general.name
  topic_name                           = azurerm_servicebus_topic.updated-searchable-donors.name
  auto_delete_on_idle                  = local.service-bus.long-expiry
  default_message_ttl                  = local.service-bus.long-expiry
  lock_duration                        = local.service-bus.default-read-lock
  max_delivery_count                   = local.service-bus.default-message-retries
  dead_lettering_on_message_expiration = true
}

resource "azurerm_servicebus_queue" "matching-requests" {
  name                                 = "matching-requests"
  resource_group_name                  = azurerm_resource_group.atlas_resource_group.name
  namespace_name                       = azurerm_servicebus_namespace.general.name
  lock_duration                        = local.service-bus.default-read-lock
  max_delivery_count                   = local.service-bus.default-message-retries
  dead_lettering_on_message_expiration = false
}

// Add audit subscriptions to all the Topics.
// Identical settings
resource "azurerm_servicebus_subscription" "audit-notifications" {
  name                                 = "audit"
  resource_group_name                  = azurerm_resource_group.atlas_resource_group.name
  namespace_name                       = azurerm_servicebus_namespace.notifications.name
  topic_name                           = azurerm_servicebus_topic.notifications.name
  auto_delete_on_idle                  = local.service-bus.audit-subscription-idle-delete
  default_message_ttl                  = local.service-bus.long-expiry
  lock_duration                        = local.service-bus.default-read-lock
  max_delivery_count                   = local.service-bus.default-message-retries
  dead_lettering_on_message_expiration = false
}

resource "azurerm_servicebus_subscription" "audit-alerts" {
  name                                 = "audit"
  resource_group_name                  = azurerm_resource_group.atlas_resource_group.name
  namespace_name                       = azurerm_servicebus_namespace.notifications.name
  topic_name                           = azurerm_servicebus_topic.alerts.name
  auto_delete_on_idle                  = local.service-bus.audit-subscription-idle-delete
  default_message_ttl                  = local.service-bus.long-expiry
  lock_duration                        = local.service-bus.default-read-lock
  max_delivery_count                   = local.service-bus.default-message-retries
  dead_lettering_on_message_expiration = false
}

resource "azurerm_servicebus_subscription" "audit-matching-results-ready" {
  name                                 = "audit"
  resource_group_name                  = azurerm_resource_group.atlas_resource_group.name
  namespace_name                       = azurerm_servicebus_namespace.general.name
  topic_name                           = azurerm_servicebus_topic.matching-results-ready.name
  auto_delete_on_idle                  = local.service-bus.audit-subscription-idle-delete
  default_message_ttl                  = local.service-bus.long-expiry
  lock_duration                        = local.service-bus.default-read-lock
  max_delivery_count                   = local.service-bus.default-message-retries
  dead_lettering_on_message_expiration = false
}

resource "azurerm_servicebus_subscription" "audit-updated-searchable-donors" {
  name                                 = "audit"
  resource_group_name                  = azurerm_resource_group.atlas_resource_group.name
  namespace_name                       = azurerm_servicebus_namespace.general.name
  topic_name                           = azurerm_servicebus_topic.updated-searchable-donors.name
  auto_delete_on_idle                  = local.service-bus.audit-subscription-idle-delete
  default_message_ttl                  = local.service-bus.long-expiry
  lock_duration                        = local.service-bus.default-read-lock
  max_delivery_count                   = local.service-bus.default-message-retries
  dead_lettering_on_message_expiration = false
}

