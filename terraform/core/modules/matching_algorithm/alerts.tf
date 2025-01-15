resource "azurerm_monitor_action_group" "atlas_alert_group" {
  name                = "Alert Atlas ${"local.environment}-ATLAS-RESOURCE-GROUP"}"
  resource_group_name = var.resource_group.name
  short_name          = "Alert Atlas"
}

resource "azurerm_monitor_metric_alert" "service_bus_dead_letter_alert" {

  description         = "There are deadletters in Matching Results Ready, Topic: Repeat Search"
  name                = "WMDA Atlas - Service Bus - Dead letter messages in matching-results-ready/repeat-search topic"
  resource_group_name = azurerm_monitor_action_group.atlas_alert_group.resource_group_name
  scopes              = [var.servicebus_namespace.id]
  severity            = 1
  window_size         = "PT1M"
  action {
    action_group_id = azurerm_monitor_action_group.atlas_alert_group.id
  }
  criteria {
    aggregation      = "Maximum"
    metric_name      = "DeadletteredMessages"
    metric_namespace = "Microsoft.ServiceBus/namespaces"
    operator         = "GreaterThan"
    threshold        = 0
    dimension {
      name     = "EntityName"
      operator = "Include"
      values   = azurerm_monitor_action_group.atlas_alert_group.name
    }
  }
}