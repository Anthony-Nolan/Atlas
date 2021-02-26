resource "azurerm_servicebus_topic" "repeat-search-matching-results-ready" {
  name                  = "repeat-search-matching-results-ready"
  resource_group_name   = var.app_service_plan.resource_group_name
  namespace_name        = var.servicebus_namespace.name
  auto_delete_on_idle   = var.default_servicebus_settings.long-expiry
  default_message_ttl   = var.default_servicebus_settings.long-expiry
  max_size_in_megabytes = var.default_servicebus_settings.default-bus-size
  support_ordering      = true
}

resource "azurerm_servicebus_subscription" "audit-repeat-search-matching-results" {
  name                                 = "audit"
  resource_group_name                  = var.app_service_plan.resource_group_name
  namespace_name                       = var.servicebus_namespace.name
  topic_name                           = azurerm_servicebus_topic.repeat-search-matching-results-ready.name
  auto_delete_on_idle                  = var.default_servicebus_settings.long-expiry
  default_message_ttl                  = var.default_servicebus_settings.long-expiry
  lock_duration                        = var.default_servicebus_settings.default-read-lock
  max_delivery_count                   = var.default_servicebus_settings.default-message-retries
  dead_lettering_on_message_expiration = false
}

resource "azurerm_servicebus_subscription" "match-prediction-orchestration-repeat-search-results-ready" {
  name                                 = "match-prediction-orchestration"
  resource_group_name                  = var.app_service_plan.resource_group_name
  namespace_name                       = var.servicebus_namespace.name
  topic_name                           = azurerm_servicebus_topic.repeat-search-matching-results-ready.name
  auto_delete_on_idle                  = var.default_servicebus_settings.long-expiry
  default_message_ttl                  = var.default_servicebus_settings.long-expiry
  lock_duration                        = var.default_servicebus_settings.default-read-lock
  max_delivery_count                   = var.default_servicebus_settings.default-message-retries
  dead_lettering_on_message_expiration = false
}

resource "azurerm_servicebus_topic" "repeat-search-results-ready" {
  name                  = "repeat-search-results-ready"
  resource_group_name   = var.app_service_plan.resource_group_name
  namespace_name        = var.servicebus_namespace.name
  auto_delete_on_idle   = var.default_servicebus_settings.long-expiry
  default_message_ttl   = var.default_servicebus_settings.long-expiry
  max_size_in_megabytes = var.default_servicebus_settings.default-bus-size
  support_ordering      = true
}

resource "azurerm_servicebus_subscription" "audit-repeat-search-results" {
  name                                 = "audit"
  resource_group_name                  = var.app_service_plan.resource_group_name
  namespace_name                       = var.servicebus_namespace.name
  topic_name                           = azurerm_servicebus_topic.repeat-search-results-ready.name
  auto_delete_on_idle                  = var.default_servicebus_settings.long-expiry
  default_message_ttl                  = var.default_servicebus_settings.long-expiry
  lock_duration                        = var.default_servicebus_settings.default-read-lock
  max_delivery_count                   = var.default_servicebus_settings.default-message-retries
  dead_lettering_on_message_expiration = false
}

resource "azurerm_servicebus_topic" "repeat-search-requests" {
  name                  = "repeat-search-requests"
  resource_group_name   = var.app_service_plan.resource_group_name
  namespace_name        = var.servicebus_namespace.name
  auto_delete_on_idle   = var.default_servicebus_settings.long-expiry
  default_message_ttl   = var.default_servicebus_settings.long-expiry
  max_size_in_megabytes = var.default_servicebus_settings.default-bus-size
  support_ordering      = true
}

resource "azurerm_servicebus_subscription" "audit-repeat-search-requests" {
  name                                 = "audit"
  resource_group_name                  = var.app_service_plan.resource_group_name
  namespace_name                       = var.servicebus_namespace.name
  topic_name                           = azurerm_servicebus_topic.repeat-search-requests.name
  auto_delete_on_idle                  = var.default_servicebus_settings.long-expiry
  default_message_ttl                  = var.default_servicebus_settings.long-expiry
  lock_duration                        = var.default_servicebus_settings.default-read-lock
  max_delivery_count                   = var.default_servicebus_settings.default-message-retries
  dead_lettering_on_message_expiration = false
}

resource "azurerm_servicebus_subscription" "repeat-search-repeat-search-requests" {
  name                                 = "repeat-search"
  resource_group_name                  = var.app_service_plan.resource_group_name
  namespace_name                       = var.servicebus_namespace.name
  topic_name                           = azurerm_servicebus_topic.repeat-search-requests.name
  auto_delete_on_idle                  = var.default_servicebus_settings.long-expiry
  default_message_ttl                  = var.default_servicebus_settings.long-expiry
  lock_duration                        = var.default_servicebus_settings.default-read-lock
  max_delivery_count                   = var.default_servicebus_settings.default-message-retries
  dead_lettering_on_message_expiration = false
}