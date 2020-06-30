resource "azurerm_servicebus_subscription" "matching_transient_a" {
  name                                 = "matching-transient-a"
  resource_group_name                  = var.app_service_plan.resource_group_name
  namespace_name                       = var.servicebus_namespace.name
  topic_name                           = var.servicebus_topics.updated-searchable-donors.name
  auto_delete_on_idle                  = var.default_servicebus_settings.long-expiry
  default_message_ttl                  = var.default_servicebus_settings.long-expiry
  lock_duration                        = var.default_servicebus_settings.default-read-lock
  max_delivery_count                   = var.default_servicebus_settings.default-message-retries
  dead_lettering_on_message_expiration = true
}

resource "azurerm_servicebus_subscription" "matching_transient_a" {
  name                                 = "matching-transient-b"
  resource_group_name                  = var.app_service_plan.resource_group_name
  namespace_name                       = var.servicebus_namespace.name
  topic_name                           = var.servicebus_topics.updated-searchable-donors.name
  auto_delete_on_idle                  = var.default_servicebus_settings.long-expiry
  default_message_ttl                  = var.default_servicebus_settings.long-expiry
  lock_duration                        = var.default_servicebus_settings.default-read-lock
  max_delivery_count                   = var.default_servicebus_settings.default-message-retries
  dead_lettering_on_message_expiration = true
}

resource "azurerm_servicebus_queue" "matching-requests" {
  name                                 = "matching-requests"
  resource_group_name                  = var.app_service_plan.resource_group_name
  namespace_name                       = var.servicebus_namespace.name
  lock_duration                        = var.default_servicebus_settings.default-read-lock
  max_delivery_count                   = var.default_servicebus_settings.default-message-retries
  dead_lettering_on_message_expiration = false
}

resource "azurerm_servicebus_topic" "matching-results-ready" {
  name                  = "matching-results-ready"
  resource_group_name   = var.app_service_plan.resource_group_name
  namespace_name        = var.servicebus_namespace.name
  auto_delete_on_idle   = var.default_servicebus_settings.long-expiry
  default_message_ttl   = var.default_servicebus_settings.long-expiry
  max_size_in_megabytes = var.default_servicebus_settings.default-bus-size
  support_ordering      = true
}

resource "azurerm_servicebus_subscription" "audit-matching-results-ready" {
  name                                 = "audit"
  resource_group_name                  = var.app_service_plan.resource_group_name
  namespace_name                       = var.servicebus_namespace.name
  topic_name                           = azurerm_servicebus_topic.matching-results-ready.name
  auto_delete_on_idle                  = var.default_servicebus_settings.audit-subscription-idle-delete
  default_message_ttl                  = var.default_servicebus_settings.long-expiry
  lock_duration                        = var.default_servicebus_settings.default-read-lock
  max_delivery_count                   = var.default_servicebus_settings.default-message-retries
  dead_lettering_on_message_expiration = false
}