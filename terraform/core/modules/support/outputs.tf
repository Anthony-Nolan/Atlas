output "general" {
  value = {
    alerts_servicebus_topic        = azurerm_servicebus_topic.alerts
    notifications_servicebus_topic = azurerm_servicebus_topic.notifications
  }
}