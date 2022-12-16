resource "azurerm_servicebus_topic" "updated-searchable-donors" {
  name                  = "updated-searchable-donors"
  namespace_id          = var.servicebus_namespace.id
  auto_delete_on_idle   = var.default_servicebus_settings.long-expiry
  default_message_ttl   = var.default_servicebus_settings.long-expiry
  max_size_in_megabytes = var.default_servicebus_settings.default-bus-size
  support_ordering      = true
}

resource "azurerm_servicebus_subscription" "audit-updated-searchable-donors" {
  name                                 = "audit"
  topic_id                             = azurerm_servicebus_topic.updated-searchable-donors.id
  auto_delete_on_idle                  = var.default_servicebus_settings.long-expiry
  default_message_ttl                  = var.default_servicebus_settings.audit-subscription-ttl-expiry
  lock_duration                        = var.default_servicebus_settings.default-read-lock
  max_delivery_count                   = var.default_servicebus_settings.default-message-retries
  dead_lettering_on_message_expiration = false
}

resource "azurerm_servicebus_topic" "donor-import-file-uploads" {
  name                  = "donor-import-file-uploads"
  namespace_id          = var.servicebus_namespace.id
  auto_delete_on_idle   = var.default_servicebus_settings.long-expiry
  default_message_ttl   = var.default_servicebus_settings.long-expiry
  max_size_in_megabytes = var.default_servicebus_settings.default-bus-size
  support_ordering      = true
}

resource "azurerm_servicebus_subscription" "donor-import-file-processor" {
  name                                 = "donor-import"
  topic_id                             = azurerm_servicebus_topic.donor-import-file-uploads.id
  auto_delete_on_idle                  = var.default_servicebus_settings.long-expiry
  default_message_ttl                  = var.default_servicebus_settings.long-expiry
  lock_duration                        = var.default_servicebus_settings.default-read-lock
  max_delivery_count                   = var.default_servicebus_settings.default-message-retries
  dead_lettering_on_message_expiration = false
}

resource "azurerm_servicebus_subscription" "audit-donor-import-file-upload" {
  name                                 = "audit"
  topic_id                             = azurerm_servicebus_topic.donor-import-file-uploads.id
  auto_delete_on_idle                  = var.default_servicebus_settings.long-expiry
  default_message_ttl                  = var.default_servicebus_settings.audit-subscription-ttl-expiry
  lock_duration                        = var.default_servicebus_settings.default-read-lock
  max_delivery_count                   = var.default_servicebus_settings.default-message-retries
  dead_lettering_on_message_expiration = false
}