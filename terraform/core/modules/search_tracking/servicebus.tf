resource "azurerm_servicebus_topic" "search-tracking-events" {
  name                  = "search-tracking-events"
  namespace_id          = var.servicebus_namespace.id
  auto_delete_on_idle   = var.default_servicebus_settings.long-expiry
  default_message_ttl   = var.default_servicebus_settings.long-expiry
  max_size_in_megabytes = var.default_servicebus_settings.default-bus-size
  support_ordering      = true
}

resource "azurerm_servicebus_subscription" "audit" {
  name                                 = "audit"
  topic_id                             = azurerm_servicebus_topic.search-tracking-events.id
  auto_delete_on_idle                  = var.default_servicebus_settings.long-expiry
  default_message_ttl                  = var.default_servicebus_settings.audit-subscription-short-ttl-expiry
  lock_duration                        = var.default_servicebus_settings.default-read-lock
  max_delivery_count                   = var.default_servicebus_settings.default-message-retries
  dead_lettering_on_message_expiration = false
}

resource "azurerm_servicebus_subscription" "search-tracking" {
  name                                 = "search-tracking"
  topic_id                             = azurerm_servicebus_topic.search-tracking-events.id
  auto_delete_on_idle                  = var.default_servicebus_settings.long-expiry
  default_message_ttl                  = var.default_servicebus_settings.long-expiry
  lock_duration                        = var.default_servicebus_settings.default-read-lock
  max_delivery_count                   = var.default_servicebus_settings.default-message-retries
  dead_lettering_on_message_expiration = false
}
