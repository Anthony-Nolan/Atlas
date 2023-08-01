locals {
  service-bus = {
    // Required for Topics.
    default-sku = "Standard"

    // Equivalent to 27.4 years - use on any prop that requires a long timespan before expiry
    long-expiry = "P9999D"

    // Value should be long enough to allow time for debug/support but short enough to prevent messages piling up
    audit-subscription-ttl-expiry = "P14D"

    // 5GB
    default-bus-size = 5120

    default-read-lock       = "PT5M"
    default-message-retries = 10
  }
}

resource "azurerm_servicebus_namespace" "general" {
  name                = "${lower(local.environment)}-atlas"
  location            = azurerm_resource_group.atlas_resource_group.location
  resource_group_name = azurerm_resource_group.atlas_resource_group.name
  sku                 = local.service-bus.default-sku
}

resource "azurerm_servicebus_namespace_authorization_rule" "manage" {
  name         = "manage"
  namespace_id = azurerm_servicebus_namespace.general.id

  listen = true
  send   = true
  manage = true
}

resource "azurerm_servicebus_namespace_authorization_rule" "read-only" {
  name         = "read-only"
  namespace_id = azurerm_servicebus_namespace.general.id

  listen = true
  send   = false
  manage = false
}

resource "azurerm_servicebus_namespace_authorization_rule" "write-only" {
  name         = "write-only"
  namespace_id = azurerm_servicebus_namespace.general.id

  listen = false
  send   = true
  manage = false
}

resource "azurerm_servicebus_namespace_authorization_rule" "read-write" {
  name         = "read-write"
  namespace_id = azurerm_servicebus_namespace.general.id

  listen = true
  send   = true
  manage = false
}

resource "azurerm_servicebus_topic" "search-results-ready" {
  name                  = "search-results-ready"
  namespace_id          = azurerm_servicebus_namespace.general.id
  auto_delete_on_idle   = local.service-bus.long-expiry
  default_message_ttl   = local.service-bus.long-expiry
  max_size_in_megabytes = local.service-bus.default-bus-size
  support_ordering      = true
}

resource "azurerm_servicebus_subscription" "audit-search-results-ready" {
  name                                 = "audit"
  topic_id                             = azurerm_servicebus_topic.search-results-ready.id
  auto_delete_on_idle                  = local.service-bus.long-expiry
  default_message_ttl                  = local.service-bus.audit-subscription-ttl-expiry
  lock_duration                        = local.service-bus.default-read-lock
  max_delivery_count                   = local.service-bus.default-message-retries
  dead_lettering_on_message_expiration = false
}

resource "azurerm_servicebus_subscription" "readers-search-results-ready" {
  for_each                             = toset(var.SEARCH_RESULTS_READY_SUBSCRIPTION_NAMES)
  name                                 = each.value
  topic_id                             = azurerm_servicebus_topic.search-results-ready.id
  auto_delete_on_idle                  = local.service-bus.long-expiry
  default_message_ttl                  = local.service-bus.audit-subscription-ttl-expiry
  lock_duration                        = local.service-bus.default-read-lock
  max_delivery_count                   = local.service-bus.default-message-retries
  dead_lettering_on_message_expiration = false
}

resource "azurerm_servicebus_subscription" "match-prediction-orchestration-search-results-ready" {
  name                                 = "match-prediction-orchestration"
  topic_id                             = module.matching_algorithm.service_bus.matching_results_topic.id
  auto_delete_on_idle                  = local.service-bus.long-expiry
  default_message_ttl                  = local.service-bus.long-expiry
  lock_duration                        = local.service-bus.default-read-lock
  max_delivery_count                   = local.service-bus.default-message-retries
  dead_lettering_on_message_expiration = false
}