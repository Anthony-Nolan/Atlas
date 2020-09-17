resource "azurerm_servicebus_topic" "notifications" {
  name                  = "notifications"
  resource_group_name   = var.resource_group.name
  namespace_name        = var.servicebus_namespace.name
  auto_delete_on_idle   = var.default_servicebus_settings.long-expiry
  default_message_ttl   = var.default_servicebus_settings.long-expiry
  max_size_in_megabytes = var.default_servicebus_settings.default-bus-size
}

resource "azurerm_servicebus_topic" "alerts" {
  name                  = "alerts"
  resource_group_name   = var.resource_group.name
  namespace_name        = var.servicebus_namespace.name
  auto_delete_on_idle   = var.default_servicebus_settings.long-expiry
  default_message_ttl   = var.default_servicebus_settings.long-expiry
  max_size_in_megabytes = var.default_servicebus_settings.default-bus-size
}

resource "azurerm_servicebus_subscription" "support-notifications" {
  name                                 = "support"
  topic_name                           = azurerm_servicebus_topic.notifications.name
  resource_group_name                  = var.resource_group.name
  namespace_name                       = var.servicebus_namespace.name
  auto_delete_on_idle                  = var.default_servicebus_settings.audit-subscription-idle-delete
  default_message_ttl                  = var.default_servicebus_settings.long-expiry
  lock_duration                        = var.default_servicebus_settings.default-read-lock
  max_delivery_count                   = var.default_servicebus_settings.default-message-retries
  dead_lettering_on_message_expiration = false
}

resource "azurerm_servicebus_subscription" "support-alerts" {
  name                                 = "support"
  topic_name                           = azurerm_servicebus_topic.alerts.name
  resource_group_name                  = var.resource_group.name
  namespace_name                       = var.servicebus_namespace.name
  auto_delete_on_idle                  = var.default_servicebus_settings.audit-subscription-idle-delete
  default_message_ttl                  = var.default_servicebus_settings.long-expiry
  lock_duration                        = var.default_servicebus_settings.default-read-lock
  max_delivery_count                   = var.default_servicebus_settings.default-message-retries
  dead_lettering_on_message_expiration = false
}

resource "azurerm_servicebus_subscription" "audit-notifications" {
  name                                 = "audit"
  topic_name                           = azurerm_servicebus_topic.notifications.name
  resource_group_name                  = var.resource_group.name
  namespace_name                       = var.servicebus_namespace.name
  auto_delete_on_idle                  = var.default_servicebus_settings.audit-subscription-idle-delete
  default_message_ttl                  = var.default_servicebus_settings.long-expiry
  lock_duration                        = var.default_servicebus_settings.default-read-lock
  max_delivery_count                   = var.default_servicebus_settings.default-message-retries
  dead_lettering_on_message_expiration = false
}

resource "azurerm_servicebus_subscription" "audit-alerts" {
  name                                 = "audit"
  topic_name                           = azurerm_servicebus_topic.alerts.name
  resource_group_name                  = var.resource_group.name
  namespace_name                       = var.servicebus_namespace.name
  auto_delete_on_idle                  = var.default_servicebus_settings.audit-subscription-idle-delete
  default_message_ttl                  = var.default_servicebus_settings.long-expiry
  lock_duration                        = var.default_servicebus_settings.default-read-lock
  max_delivery_count                   = var.default_servicebus_settings.default-message-retries
  dead_lettering_on_message_expiration = false
}

