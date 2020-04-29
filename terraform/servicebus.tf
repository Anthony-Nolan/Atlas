locals {
  service-bus = {
    default-sku                    = "Standard" // Required for Topics.
    long-expiry                    = "P9999D" // 2.75 years
    audit-subscription-idle-delete = "P8D"
    default-read-lock              = "PT5M"
    default-bus-size               = 5120 // 5GB
    default-message-retries        = 10
  }
}

// Historically this codebase used 2 distinct ServiceBusses; however we don't think that's necessary in practice.
// We retain the ability to separate them again in the future, but in fact have them pointed at the same namespace
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

// Topics for Notifications/Alerts, Results, and Donor Updates; Queue for Searches.
// Identical except for:
//   support_ordering for Donor Updates & Results.

resource "azurerm_servicebus_topic" "notifications" {
  name                  = "notifications"
  resource_group_name   = azurerm_resource_group.atlas_resource_group.name
  namespace_name        = azurerm_servicebus_namespace.general.name
  auto_delete_on_idle   = local.service-bus.long-expiry
  default_message_ttl   = local.service-bus.long-expiry
  max_size_in_megabytes = local.service-bus.default-bus-size
}

resource "azurerm_servicebus_topic" "alerts" {
  name                  = "alerts"
  resource_group_name   = azurerm_resource_group.atlas_resource_group.name
  namespace_name        = azurerm_servicebus_namespace.general.name
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

resource "azurerm_servicebus_subscription" "matching" {
  name                                 = "matching"
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
  namespace_name                       = azurerm_servicebus_namespace.general.name
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
  namespace_name                       = azurerm_servicebus_namespace.general.name
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

