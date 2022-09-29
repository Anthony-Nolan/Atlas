resource "azurerm_servicebus_topic" "haplotype-frequency-file-uploads" {
  name                  = "haplotype-frequency-file-uploads"
  resource_group_name   = var.app_service_plan.resource_group_name
  namespace_name        = var.servicebus_namespace.name
  auto_delete_on_idle   = var.default_servicebus_settings.long-expiry
  default_message_ttl   = var.default_servicebus_settings.long-expiry
  max_size_in_megabytes = var.default_servicebus_settings.default-bus-size
  support_ordering      = true
}

resource "azurerm_servicebus_subscription" "haplotype-frequency-file-processor" {
  name                                 = "haplotype-frequency-import"
  resource_group_name                  = var.app_service_plan.resource_group_name
  namespace_name                       = var.servicebus_namespace.name
  topic_name                           = azurerm_servicebus_topic.haplotype-frequency-file-uploads.name
  auto_delete_on_idle                  = var.default_servicebus_settings.long-expiry
  default_message_ttl                  = var.default_servicebus_settings.long-expiry
  lock_duration                        = var.default_servicebus_settings.default-read-lock
  max_delivery_count                   = var.default_servicebus_settings.default-message-retries
  dead_lettering_on_message_expiration = false
}

resource "azurerm_servicebus_subscription" "audit-haplotype-frequency-file-upload" {
  name                                 = "audit"
  resource_group_name                  = var.app_service_plan.resource_group_name
  namespace_name                       = var.servicebus_namespace.name
  topic_name                           = azurerm_servicebus_topic.haplotype-frequency-file-uploads.name
  auto_delete_on_idle                  = var.default_servicebus_settings.long-expiry
  default_message_ttl                  = var.default_servicebus_settings.long-expiry
  lock_duration                        = var.default_servicebus_settings.default-read-lock
  max_delivery_count                   = var.default_servicebus_settings.default-message-retries
  dead_lettering_on_message_expiration = false
}

resource "azurerm_servicebus_topic" "match-prediction-requests" {
  name                  = "match-prediction-requests"
  resource_group_name   = var.app_service_plan.resource_group_name
  namespace_name        = var.servicebus_namespace.name
  auto_delete_on_idle   = var.default_servicebus_settings.long-expiry
  default_message_ttl   = var.default_servicebus_settings.long-expiry
  max_size_in_megabytes = var.default_servicebus_settings.default-bus-size
  support_ordering      = true
}

resource "azurerm_servicebus_subscription" "match-prediction-request-runner" {
  name                                 = "match-prediction"
  resource_group_name                  = var.app_service_plan.resource_group_name
  namespace_name                       = var.servicebus_namespace.name
  topic_name                           = azurerm_servicebus_topic.match-prediction-requests.name
  auto_delete_on_idle                  = var.default_servicebus_settings.long-expiry
  default_message_ttl                  = var.default_servicebus_settings.long-expiry
  lock_duration                        = var.default_servicebus_settings.default-read-lock
  max_delivery_count                   = var.default_servicebus_settings.default-message-retries
  dead_lettering_on_message_expiration = false
}

resource "azurerm_servicebus_topic" "match-prediction-results" {
  name                  = "match-prediction-results"
  resource_group_name   = var.app_service_plan.resource_group_name
  namespace_name        = var.servicebus_namespace.name
  auto_delete_on_idle   = var.default_servicebus_settings.long-expiry
  default_message_ttl   = var.default_servicebus_settings.long-expiry
  max_size_in_megabytes = var.default_servicebus_settings.default-bus-size
  support_ordering      = true
}
