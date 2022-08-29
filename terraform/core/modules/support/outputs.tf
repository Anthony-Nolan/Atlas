output "general" {
  value = {
    alerts_servicebus_topic_id        = azurerm_servicebus_topic.alerts.id
    alerts_servicebus_topic           = azurerm_servicebus_topic.alerts
    notifications_servicebus_topic_id = azurerm_servicebus_topic.notifications.id
    notifications_servicebus_topic    = azurerm_servicebus_topic.notifications
  }
}