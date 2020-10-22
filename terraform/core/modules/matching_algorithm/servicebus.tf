resource "azurerm_servicebus_subscription" "matching_transient_a" {
  name                                 = "matching-transient-a"
  resource_group_name                  = var.resource_group.name
  namespace_name                       = var.servicebus_namespace.name
  topic_name                           = var.servicebus_topics.updated-searchable-donors.name
  auto_delete_on_idle                  = var.default_servicebus_settings.long-expiry
  default_message_ttl                  = var.default_servicebus_settings.long-expiry
  lock_duration                        = var.default_servicebus_settings.default-read-lock
  max_delivery_count                   = var.default_servicebus_settings.default-message-retries
  dead_lettering_on_message_expiration = true
}

resource "azurerm_servicebus_subscription" "matching_transient_b" {
  name                                 = "matching-transient-b"
  resource_group_name                  = var.resource_group.name
  namespace_name                       = var.servicebus_namespace.name
  topic_name                           = var.servicebus_topics.updated-searchable-donors.name
  auto_delete_on_idle                  = var.default_servicebus_settings.long-expiry
  default_message_ttl                  = var.default_servicebus_settings.long-expiry
  lock_duration                        = var.default_servicebus_settings.default-read-lock
  max_delivery_count                   = var.default_servicebus_settings.default-message-retries
  dead_lettering_on_message_expiration = true
}

resource "azurerm_servicebus_topic" "matching-requests" {
  name                  = "matching-requests"
  resource_group_name   = var.resource_group.name
  namespace_name        = var.servicebus_namespace.name
  auto_delete_on_idle   = var.default_servicebus_settings.long-expiry
  default_message_ttl   = var.default_servicebus_settings.long-expiry
  max_size_in_megabytes = var.default_servicebus_settings.default-bus-size
  support_ordering      = true
}

resource "azurerm_servicebus_subscription" "audit-matching-requests" {
  name                                 = "audit"
  resource_group_name                  = var.resource_group.name
  namespace_name                       = var.servicebus_namespace.name
  topic_name                           = azurerm_servicebus_topic.matching-requests.name
  auto_delete_on_idle                  = var.default_servicebus_settings.long-expiry
  default_message_ttl                  = var.default_servicebus_settings.long-expiry
  lock_duration                        = var.default_servicebus_settings.default-read-lock
  max_delivery_count                   = var.default_servicebus_settings.default-message-retries
  dead_lettering_on_message_expiration = false
}

resource "azurerm_servicebus_subscription" "matching-requests-matching-algorithm" {
  name                                 = "matching-algorithm"
  resource_group_name                  = var.resource_group.name
  namespace_name                       = var.servicebus_namespace.name
  topic_name                           = azurerm_servicebus_topic.matching-requests.name
  auto_delete_on_idle                  = var.default_servicebus_settings.long-expiry
  default_message_ttl                  = var.default_servicebus_settings.long-expiry
  lock_duration                        = var.default_servicebus_settings.default-read-lock
  max_delivery_count                   = var.default_servicebus_settings.default-message-retries
  dead_lettering_on_message_expiration = false
}

resource "azurerm_servicebus_topic" "matching-results-ready" {
  name                  = "matching-results-ready"
  resource_group_name   = var.resource_group.name
  namespace_name        = var.servicebus_namespace.name
  auto_delete_on_idle   = var.default_servicebus_settings.long-expiry
  default_message_ttl   = var.default_servicebus_settings.long-expiry
  max_size_in_megabytes = var.default_servicebus_settings.default-bus-size
  support_ordering      = true
}

resource "azurerm_servicebus_subscription" "audit-matching-results-ready" {
  name                                 = "audit"
  resource_group_name                  = var.resource_group.name
  namespace_name                       = var.servicebus_namespace.name
  topic_name                           = azurerm_servicebus_topic.matching-results-ready.name
  auto_delete_on_idle                  = var.default_servicebus_settings.long-expiry
  default_message_ttl                  = var.default_servicebus_settings.long-expiry
  lock_duration                        = var.default_servicebus_settings.default-read-lock
  max_delivery_count                   = var.default_servicebus_settings.default-message-retries
  dead_lettering_on_message_expiration = false
}

resource "azurerm_servicebus_topic" "data-refresh-requests" {
  name                  = "data-refresh-requests"
  resource_group_name   = var.resource_group.name
  namespace_name        = var.servicebus_namespace.name
  auto_delete_on_idle   = var.default_servicebus_settings.long-expiry
  default_message_ttl   = var.default_servicebus_settings.long-expiry
  max_size_in_megabytes = var.default_servicebus_settings.default-bus-size
  support_ordering      = true
}

resource "azurerm_servicebus_subscription" "audit-data-refresh-requests" {
  name                                 = "audit"
  resource_group_name                  = var.resource_group.name
  namespace_name                       = var.servicebus_namespace.name
  topic_name                           = azurerm_servicebus_topic.data-refresh-requests.name
  auto_delete_on_idle                  = var.default_servicebus_settings.long-expiry
  default_message_ttl                  = var.default_servicebus_settings.long-expiry
  lock_duration                        = var.default_servicebus_settings.default-read-lock
  max_delivery_count                   = var.default_servicebus_settings.default-message-retries
  dead_lettering_on_message_expiration = false
}

resource "azurerm_servicebus_subscription" "matching-algorithm-data-refresh-requests" {
  name                                 = "matching-algorithm"
  resource_group_name                  = var.resource_group.name
  namespace_name                       = var.servicebus_namespace.name
  topic_name                           = azurerm_servicebus_topic.data-refresh-requests.name
  auto_delete_on_idle                  = var.default_servicebus_settings.long-expiry
  default_message_ttl                  = var.default_servicebus_settings.long-expiry
  lock_duration                        = var.default_servicebus_settings.default-read-lock
  max_delivery_count                   = var.default_servicebus_settings.default-message-retries
  dead_lettering_on_message_expiration = false
}

resource "azurerm_servicebus_topic" "completed-data-refresh-jobs" {
  name                  = "completed-data-refresh-jobs"
  resource_group_name   = var.resource_group.name
  namespace_name        = var.servicebus_namespace.name
  auto_delete_on_idle   = var.default_servicebus_settings.long-expiry
  default_message_ttl   = var.default_servicebus_settings.long-expiry
  max_size_in_megabytes = var.default_servicebus_settings.default-bus-size
  support_ordering      = true
}

resource "azurerm_servicebus_subscription" "audit-completed-data-refresh-jobs" {
  name                                 = "audit"
  resource_group_name                  = var.resource_group.name
  namespace_name                       = var.servicebus_namespace.name
  topic_name                           = azurerm_servicebus_topic.completed-data-refresh-jobs.name
  auto_delete_on_idle                  = var.default_servicebus_settings.long-expiry
  default_message_ttl                  = var.default_servicebus_settings.long-expiry
  lock_duration                        = var.default_servicebus_settings.default-read-lock
  max_delivery_count                   = var.default_servicebus_settings.default-message-retries
  dead_lettering_on_message_expiration = false
}