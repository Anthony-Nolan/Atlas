// Full copy of the real things, but combined into a single NS, for convenience.
// We expect this to be deployed to the Dev environment, since Local ServiceBuses are (easily) AThing(TM)

// "Standard Tier" is required for Topics.
resource "azurerm_servicebus_namespace" "local-dev" {
  name                = "local-atlas-combined"
  location            = azurerm_resource_group.atlas_resource_group.location
  resource_group_name = azurerm_resource_group.atlas_resource_group.name
  sku                 = "Standard"
}

// Topics for Notifications, Results, and Donor Updates; Queue for Searches.
// enable_express for Searches.
// support_ordering for donor updates.

resource "azurerm_servicebus_topic" "notifications" {
  name                = "notifications"
  resource_group_name = azurerm_resource_group.atlas_resource_group.name
  namespace_name      = azurerm_servicebus_namespace.local-dev.name
  auto_delete_on_idle = "P9999D" //2.75 years
  default_message_ttl = "P9999D" //2.75 years
  max_size_in_megabytes = 5120   //5 GB
}

resource "azurerm_servicebus_topic" "alerts" {
  name                = "alerts"
  resource_group_name = azurerm_resource_group.atlas_resource_group.name
  namespace_name      = azurerm_servicebus_namespace.local-dev.name
  auto_delete_on_idle = "P9999D" //2.75 years
  default_message_ttl = "P9999D" //2.75 years
  max_size_in_megabytes = 5120   //5 GB
}

resource "azurerm_servicebus_topic" "matching-results-ready" {
  name                = "matching-results-ready"
  resource_group_name = azurerm_resource_group.atlas_resource_group.name
  namespace_name      = azurerm_servicebus_namespace.local-dev.name
  auto_delete_on_idle = "P9999D" //2.75 years
  default_message_ttl = "P9999D" //2.75 years
  max_size_in_megabytes = 5120   //5 GB
  enable_express      = true
}

resource "azurerm_servicebus_topic" "updated-searchable-donors" {
  name                = "updated-searchable-donors"
  resource_group_name = azurerm_resource_group.atlas_resource_group.name
  namespace_name      = azurerm_servicebus_namespace.local-dev.name
  auto_delete_on_idle = "P9999D" //2.75 years
  default_message_ttl = "P9999D" //2.75 years
  max_size_in_megabytes = 5120   //5 GB
  support_ordering    = true
}

resource "azurerm_servicebus_subscription" "donor-update" {
  name                = "donor-update"
  resource_group_name = azurerm_resource_group.atlas_resource_group.name
  namespace_name      = azurerm_servicebus_namespace.local-dev.name
  topic_name          = azurerm_servicebus_topic.updated-searchable-donors.name
  auto_delete_on_idle = "P9999D" //2.75 years
  default_message_ttl = "P9999D" //2.75 years
  lock_duration       = "PT5M"
  max_delivery_count  = 10
  dead_lettering_on_message_expiration = true
}

resource "azurerm_servicebus_queue" "matching-requests" {
  name                = "matching-requests"
  resource_group_name = azurerm_resource_group.atlas_resource_group.name
  namespace_name      = azurerm_servicebus_namespace.local-dev.name
  lock_duration       = "PT5M"
  max_delivery_count  = 10
  dead_lettering_on_message_expiration = false
}

// Add audit subscriptions to all the Topics.
// Identical settings
resource "azurerm_servicebus_subscription" "audit-notifications" {
  name                = "audit"
  resource_group_name = azurerm_resource_group.atlas_resource_group.name
  namespace_name      = azurerm_servicebus_namespace.local-dev.name
  topic_name          = azurerm_servicebus_topic.notifications.name
  auto_delete_on_idle = "P8D"   
  default_message_ttl = "P9999D" //2.75 years
  lock_duration       = "PT5M"
  max_delivery_count  = 10
  dead_lettering_on_message_expiration = true
}

resource "azurerm_servicebus_subscription" "audit-alerts" {
  name                = "audit"
  resource_group_name = azurerm_resource_group.atlas_resource_group.name
  namespace_name      = azurerm_servicebus_namespace.local-dev.name
  topic_name          = azurerm_servicebus_topic.alerts.name
  auto_delete_on_idle = "P8D"   
  default_message_ttl = "P9999D" //2.75 years
  lock_duration       = "PT5M"
  max_delivery_count  = 10
  dead_lettering_on_message_expiration = true
}

resource "azurerm_servicebus_subscription" "audit-matching-results-ready" {
  name                = "audit"
  resource_group_name = azurerm_resource_group.atlas_resource_group.name
  namespace_name      = azurerm_servicebus_namespace.local-dev.name
  topic_name          = azurerm_servicebus_topic.matching-results-ready.name
  auto_delete_on_idle = "P8D"   
  default_message_ttl = "P9999D" //2.75 years
  lock_duration       = "PT5M"
  max_delivery_count  = 10
  dead_lettering_on_message_expiration = true
}

resource "azurerm_servicebus_subscription" "audit-updated-searchable-donors" {
  name                = "audit"
  resource_group_name = azurerm_resource_group.atlas_resource_group.name
  namespace_name      = azurerm_servicebus_namespace.local-dev.name
  topic_name          = azurerm_servicebus_topic.updated-searchable-donors.name
  auto_delete_on_idle = "P8D"   
  default_message_ttl = "P9999D" //2.75 years
  lock_duration       = "PT5M"
  max_delivery_count  = 10
  dead_lettering_on_message_expiration = true
}
