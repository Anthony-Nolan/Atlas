output "general" {
  value = {
    alerts_servicebus_topic                     = azurerm_servicebus_topic.alerts
    alerts_servicebus_debug_subscription        = azurerm_servicebus_subscription.debug-alerts.name
    notifications_servicebus_topic              = azurerm_servicebus_topic.notifications
    notifications_servicebus_debug_subscription = azurerm_servicebus_subscription.debug-notifications.name
  }
}