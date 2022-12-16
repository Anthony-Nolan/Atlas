# Repeat search - matching-only results

resource "azurerm_servicebus_topic" "repeat-search-matching-results-ready" {
  name                  = "repeat-search-matching-results-ready"
  namespace_id          = var.servicebus_namespace.id
  auto_delete_on_idle   = var.default_servicebus_settings.long-expiry
  default_message_ttl   = var.default_servicebus_settings.long-expiry
  max_size_in_megabytes = var.default_servicebus_settings.default-bus-size
  support_ordering      = true
}

resource "azurerm_servicebus_subscription" "audit-repeat-search-matching-results" {
  name                                 = "audit"
  topic_id                             = azurerm_servicebus_topic.repeat-search-matching-results-ready.id
  auto_delete_on_idle                  = var.default_servicebus_settings.long-expiry
  default_message_ttl                  = var.default_servicebus_settings.audit-subscription-ttl-expiry
  lock_duration                        = var.default_servicebus_settings.default-read-lock
  max_delivery_count                   = var.default_servicebus_settings.default-message-retries
  dead_lettering_on_message_expiration = false
}

resource "azurerm_servicebus_subscription" "match-prediction-orchestration-repeat-search-results-ready" {
  name                                 = "match-prediction-orchestration"
  topic_id                             = azurerm_servicebus_topic.repeat-search-matching-results-ready.id
  auto_delete_on_idle                  = var.default_servicebus_settings.long-expiry
  default_message_ttl                  = var.default_servicebus_settings.long-expiry
  lock_duration                        = var.default_servicebus_settings.default-read-lock
  max_delivery_count                   = var.default_servicebus_settings.default-message-retries
  dead_lettering_on_message_expiration = false
}

# Repeat search - full results

resource "azurerm_servicebus_topic" "repeat-search-results-ready" {
  name                  = "repeat-search-results-ready"
  namespace_id          = var.servicebus_namespace.id
  auto_delete_on_idle   = var.default_servicebus_settings.long-expiry
  default_message_ttl   = var.default_servicebus_settings.long-expiry
  max_size_in_megabytes = var.default_servicebus_settings.default-bus-size
  support_ordering      = true
}

resource "azurerm_servicebus_subscription" "audit-repeat-search-results" {
  name                                 = "audit"
  topic_id                             = azurerm_servicebus_topic.repeat-search-results-ready.id
  auto_delete_on_idle                  = var.default_servicebus_settings.long-expiry
  default_message_ttl                  = var.default_servicebus_settings.audit-subscription-ttl-expiry
  lock_duration                        = var.default_servicebus_settings.default-read-lock
  max_delivery_count                   = var.default_servicebus_settings.default-message-retries
  dead_lettering_on_message_expiration = false
}

# Repeat search - requests

resource "azurerm_servicebus_topic" "repeat-search-requests" {
  name                  = "repeat-search-requests"
  namespace_id          = var.servicebus_namespace.id
  auto_delete_on_idle   = var.default_servicebus_settings.long-expiry
  default_message_ttl   = var.default_servicebus_settings.long-expiry
  max_size_in_megabytes = var.default_servicebus_settings.default-bus-size
  support_ordering      = true
}

resource "azurerm_servicebus_subscription" "audit-repeat-search-requests" {
  name                                 = "audit"
  topic_id                             = azurerm_servicebus_topic.repeat-search-requests.id
  auto_delete_on_idle                  = var.default_servicebus_settings.long-expiry
  default_message_ttl                  = var.default_servicebus_settings.audit-subscription-ttl-expiry
  lock_duration                        = var.default_servicebus_settings.default-read-lock
  max_delivery_count                   = var.default_servicebus_settings.default-message-retries
  dead_lettering_on_message_expiration = false
}

resource "azurerm_servicebus_subscription" "repeat-search-repeat-search-requests" {
  name                                 = "repeat-search"
  topic_id                             = azurerm_servicebus_topic.repeat-search-requests.id
  auto_delete_on_idle                  = var.default_servicebus_settings.long-expiry
  default_message_ttl                  = var.default_servicebus_settings.long-expiry
  lock_duration                        = var.default_servicebus_settings.default-read-lock
  max_delivery_count                   = var.default_servicebus_settings.default-message-retries
  dead_lettering_on_message_expiration = false
}

# Original search - results listener

resource "azurerm_servicebus_subscription" "original-search-results-ready-repeat-search-listener" {
  name                                 = "repeat-search"
  topic_id                             = var.original-search-matching-results-topic.id
  auto_delete_on_idle                  = var.default_servicebus_settings.long-expiry
  default_message_ttl                  = var.default_servicebus_settings.long-expiry
  lock_duration                        = var.default_servicebus_settings.default-read-lock
  max_delivery_count                   = var.default_servicebus_settings.default-message-retries
  dead_lettering_on_message_expiration = false
}
