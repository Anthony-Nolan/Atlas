resource "azurerm_servicebus_topic" "notifications" {
  name                  = "notifications"
  namespace_id          = var.servicebus_namespace.id
  auto_delete_on_idle   = var.default_servicebus_settings.long-expiry
  default_message_ttl   = var.default_servicebus_settings.long-expiry
  max_size_in_megabytes = var.default_servicebus_settings.default-bus-size
}

resource "azurerm_servicebus_topic" "alerts" {
  name                  = "alerts"
  namespace_id          = var.servicebus_namespace.id
  auto_delete_on_idle   = var.default_servicebus_settings.long-expiry
  default_message_ttl   = var.default_servicebus_settings.long-expiry
  max_size_in_megabytes = var.default_servicebus_settings.default-bus-size
}

resource "azurerm_servicebus_subscription" "support-notifications" {
  name                                 = "support"
  topic_id                             = azurerm_servicebus_topic.notifications.id
  auto_delete_on_idle                  = var.default_servicebus_settings.long-expiry
  default_message_ttl                  = var.default_servicebus_settings.long-expiry
  lock_duration                        = var.default_servicebus_settings.default-read-lock
  max_delivery_count                   = var.default_servicebus_settings.default-message-retries
  dead_lettering_on_message_expiration = false
}

resource "azurerm_servicebus_subscription" "support-alerts" {
  name                                 = "support"
  topic_id                             = azurerm_servicebus_topic.alerts.id
  auto_delete_on_idle                  = var.default_servicebus_settings.long-expiry
  default_message_ttl                  = var.default_servicebus_settings.long-expiry
  lock_duration                        = var.default_servicebus_settings.default-read-lock
  max_delivery_count                   = var.default_servicebus_settings.default-message-retries
  dead_lettering_on_message_expiration = false
}

resource "azurerm_servicebus_subscription" "audit-notifications" {
  name                                 = "audit"
  topic_id                             = azurerm_servicebus_topic.notifications.id
  auto_delete_on_idle                  = var.default_servicebus_settings.long-expiry
  default_message_ttl                  = var.default_servicebus_settings.audit-subscription-ttl-expiry
  lock_duration                        = var.default_servicebus_settings.default-read-lock
  max_delivery_count                   = var.default_servicebus_settings.default-message-retries
  dead_lettering_on_message_expiration = false
}

resource "azurerm_servicebus_subscription" "audit-alerts" {
  name                                 = "audit"
  topic_id                             = azurerm_servicebus_topic.alerts.id
  auto_delete_on_idle                  = var.default_servicebus_settings.long-expiry
  default_message_ttl                  = var.default_servicebus_settings.audit-subscription-ttl-expiry
  lock_duration                        = var.default_servicebus_settings.default-read-lock
  max_delivery_count                   = var.default_servicebus_settings.default-message-retries
  dead_lettering_on_message_expiration = false
}

resource "azurerm_servicebus_subscription" "debug-notifications" {
  name                                 = "debug"
  topic_id                             = azurerm_servicebus_topic.notifications.id
  auto_delete_on_idle                  = var.default_servicebus_settings.long-expiry
  default_message_ttl                  = var.default_servicebus_settings.debug-subscription-ttl-expiry
  lock_duration                        = var.default_servicebus_settings.default-read-lock
  max_delivery_count                   = var.default_servicebus_settings.default-message-retries
  dead_lettering_on_message_expiration = false
}

resource "azurerm_servicebus_subscription" "debug-alerts" {
  name                                 = "debug"
  topic_id                             = azurerm_servicebus_topic.alerts.id
  auto_delete_on_idle                  = var.default_servicebus_settings.long-expiry
  default_message_ttl                  = var.default_servicebus_settings.debug-subscription-ttl-expiry
  lock_duration                        = var.default_servicebus_settings.default-read-lock
  max_delivery_count                   = var.default_servicebus_settings.default-message-retries
  dead_lettering_on_message_expiration = false
}

