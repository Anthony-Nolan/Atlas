resource "azurerm_servicebus_topic" "updated-searchable-donors" {
  name                  = "updated-searchable-donors"
  resource_group_name   = var.app_service_plan.resource_group_name
  namespace_name        = var.servicebus_namespace.name
  auto_delete_on_idle   = var.default_servicebus_settings.long-expiry
  default_message_ttl   = var.default_servicebus_settings.long-expiry
  max_size_in_megabytes = var.default_servicebus_settings.default-bus-size
  support_ordering      = true
}

resource "azurerm_servicebus_subscription" "audit-updated-searchable-donors" {
  name                                 = "audit"
  resource_group_name                  = var.app_service_plan.resource_group_name
  namespace_name                       = var.servicebus_namespace.name
  topic_name                           = azurerm_servicebus_topic.updated-searchable-donors.name
  auto_delete_on_idle                  = var.default_servicebus_settings.audit-subscription-idle-delete
  default_message_ttl                  = var.default_servicebus_settings.long-expiry
  lock_duration                        = var.default_servicebus_settings.default-read-lock
  max_delivery_count                   = var.default_servicebus_settings.default-message-retries
  dead_lettering_on_message_expiration = false
}

resource "azurerm_servicebus_topic" "donor-import-file-uploads" {
  name                  = "donor-import-file-uploads"
  resource_group_name   = var.app_service_plan.resource_group_name
  namespace_name        = var.servicebus_namespace.name
  auto_delete_on_idle   = var.default_servicebus_settings.long-expiry
  default_message_ttl   = var.default_servicebus_settings.long-expiry
  max_size_in_megabytes = var.default_servicebus_settings.default-bus-size
  support_ordering      = true
}

resource "azurerm_servicebus_subscription" "donor-import-file-processor" {
  name                                 = "donor-import"
  resource_group_name                  = var.app_service_plan.resource_group_name
  namespace_name                       = var.servicebus_namespace.name
  topic_name                           = azurerm_servicebus_topic.donor-import-file-uploads.name
  auto_delete_on_idle                  = var.default_servicebus_settings.audit-subscription-idle-delete
  default_message_ttl                  = var.default_servicebus_settings.long-expiry
  lock_duration                        = var.default_servicebus_settings.default-read-lock
  max_delivery_count                   = var.default_servicebus_settings.default-message-retries
  dead_lettering_on_message_expiration = false
}

resource "azurerm_servicebus_subscription" "audit-donor-import-file-upload" {
  name                                 = "audit"
  resource_group_name                  = var.app_service_plan.resource_group_name
  namespace_name                       = var.servicebus_namespace.name
  topic_name                           = azurerm_servicebus_topic.donor-import-file-uploads.name
  auto_delete_on_idle                  = var.default_servicebus_settings.audit-subscription-idle-delete
  default_message_ttl                  = var.default_servicebus_settings.long-expiry
  lock_duration                        = var.default_servicebus_settings.default-read-lock
  max_delivery_count                   = var.default_servicebus_settings.default-message-retries
  dead_lettering_on_message_expiration = false
}