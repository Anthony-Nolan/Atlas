output "general" {
  value = {
    updated_searchable_donors_servicebus_topic = azurerm_servicebus_topic.updated-searchable-donors
  }
}