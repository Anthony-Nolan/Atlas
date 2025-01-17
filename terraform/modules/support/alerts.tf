resource "azurerm_monitor_metric_alert" "service_bus_dead_letter_alert" {
  for_each = var.service_bus_topics_with_alerts

  name                = "Atlas - Service Bus - Dead letter messages in ${each.value.name} topic"
  description         = "There are deadletters in Service Bus: ${var.servicebus_namespace.name}, Topic: ${each.value.name}"
  resource_group_name = var.resource_group.name
  scopes              = [var.servicebus_namespace.id]
  severity            = 1
  window_size         = "PT1M"
  action {
    action_group_id = azurerm_resource_group.atlas_resource_group.id
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
      values   = ["${each.value.name}"]
    }
  }
}