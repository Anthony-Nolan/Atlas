output "general" {
  value = {
    alerts_servicebus_topic                     = azurerm_servicebus_topic.alerts
    alerts_servicebus_debug_subscription        = azurerm_servicebus_subscription.debug-alerts.name
    notifications_servicebus_topic              = azurerm_servicebus_topic.notifications
    notifications_servicebus_debug_subscription = azurerm_servicebus_subscription.debug-notifications.name
    atlas_team_action_group                     = azurerm_monitor_action_group.atlas_team
  }
}