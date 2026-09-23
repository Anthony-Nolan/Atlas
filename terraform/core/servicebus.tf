locals {
  service-bus = {
    // Required for Topics.
    default-sku = "Standard"

    // Equivalent to 27.4 years - use on any prop that requires a long timespan before expiry
    long-expiry = "P9999D"

    // Value should be long enough to allow time to permit support investigations but short enough to prevent messages piling up
    audit-subscription-ttl-expiry = "P14D"

    // Value should be short enough to prevent messages piling up
    audit-subscription-short-ttl-expiry = "P1D"

    // Debug subscriptions are primarily used by quick-running automated tests, so a short message expiry is required to keep message count to a minimum.
    debug-subscription-ttl-expiry = "PT1H"

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

resource "azurerm_servicebus_subscription" "debug-search-results-ready" {
  name                                 = "debug"
  topic_id                             = azurerm_servicebus_topic.search-results-ready.id
  auto_delete_on_idle                  = local.service-bus.long-expiry
  default_message_ttl                  = local.service-bus.debug-subscription-ttl-expiry
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
// Announces that the HLA Metadata Dictionary's stored data has been recreated, so that every app holding an
// in-memory copy of that data can drop it. Both recreation routes publish here - a data refresh moving to a new
// nomenclature version, and a forced recreation at the version that is already active. The latter is the one that
// needs announcing: no version string changes, so a consumer has no other way of noticing. See ATL-395.
resource "azurerm_servicebus_topic" "hla-metadata-dictionary-updated" {
  name                  = "hla-metadata-dictionary-updated"
  namespace_id          = azurerm_servicebus_namespace.general.id
  auto_delete_on_idle   = local.service-bus.long-expiry
  default_message_ttl   = local.service-bus.long-expiry
  max_size_in_megabytes = local.service-bus.default-bus-size
  support_ordering      = true
}

// Consuming apps create a subscription per WORKER INSTANCE at startup, rather than one per app being declared here.
// A subscription is a competing-consumer queue, so a single shared subscription would hand each invalidation to one
// arbitrary instance and leave every other instance of that app serving its stale copy - and only an instance itself
// knows which instance it is. Hence manage rights, deliberately scoped to this one topic rather than granted over the
// namespace. Instance subscriptions carry an auto-delete-on-idle window, so scaled-away instances clean up after
// themselves. See HlaMetadataDictionaryCacheInvalidationConfiguration.
resource "azurerm_servicebus_topic_authorization_rule" "hla-metadata-dictionary-updated-manage" {
  name     = "manage"
  topic_id = azurerm_servicebus_topic.hla-metadata-dictionary-updated.id

  listen = true
  send   = true
  manage = true
}

resource "azurerm_servicebus_subscription" "audit-hla-metadata-dictionary-updated" {
  name                                 = "audit"
  topic_id                             = azurerm_servicebus_topic.hla-metadata-dictionary-updated.id
  auto_delete_on_idle                  = local.service-bus.long-expiry
  default_message_ttl                  = local.service-bus.audit-subscription-ttl-expiry
  lock_duration                        = local.service-bus.default-read-lock
  max_delivery_count                   = local.service-bus.default-message-retries
  dead_lettering_on_message_expiration = false
}
