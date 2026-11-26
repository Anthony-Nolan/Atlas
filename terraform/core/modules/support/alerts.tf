resource "azurerm_monitor_action_group" "atlas_team" {
  name                = "${var.general.environment}-ATLAS-AG"
  resource_group_name = var.resource_group.name
  short_name          = "AtlasAlerts"
  tags                = var.general.common_tags

  email_receiver {
    name          = "AtlasTeam"
    email_address = "atlasteam@anthonynolan.org"
  }
}

resource "azurerm_monitor_metric_alert" "service_bus_dead_letter_alert" {
  for_each = var.SUPPORT_DEADLETTER_ALERTS_ACTION_GROUP_ID != null ? var.service_bus_topics_with_alerts : {}

  name                = "Atlas - Service Bus - Dead letter messages in ${each.value.name} topic"
  description         = "There are deadletters in Service Bus: ${var.servicebus_namespace.name}, Topic: ${each.value.name}"
  resource_group_name = var.resource_group.name
  scopes              = [var.servicebus_namespace.id]
  severity            = 1
  window_size         = "PT1M"
  action {
    action_group_id = var.SUPPORT_DEADLETTER_ALERTS_ACTION_GROUP_ID
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

// Stage 65 of the data refresh fails, after many hours, if the database being refreshed reaches its max size. The
// stored donor genotype sets also stay in the database after the refresh. Alerting before the database is full leaves
// time to raise MATCHING_DATABASE_MAX_SIZE_GB.
resource "azurerm_monitor_metric_alert" "matching_transient_database_storage" {
  for_each = var.matching_transient_databases

  name                = "Atlas - Matching - Storage of ${each.value.name} above ${var.MATCHING_DATABASE_STORAGE_ALERT_THRESHOLD_PERCENT} percent of max size"
  description         = "Transient matching database ${each.value.name} has used more than ${var.MATCHING_DATABASE_STORAGE_ALERT_THRESHOLD_PERCENT}% of its max size. Raise MATCHING_DATABASE_MAX_SIZE_GB within the limit of the active, dormant and refresh tiers. See the stage 65 runbook in README_Deployment.md."
  resource_group_name = var.resource_group.name
  scopes              = [each.value.id]
  severity            = 2
  frequency           = "PT5M"
  window_size         = "PT15M"
  tags                = var.general.common_tags
  action {
    action_group_id = azurerm_monitor_action_group.atlas_team.id
  }
  criteria {
    aggregation      = "Maximum"
    metric_name      = "storage_percent"
    metric_namespace = "Microsoft.Sql/servers/databases"
    operator         = "GreaterThan"
    threshold        = var.MATCHING_DATABASE_STORAGE_ALERT_THRESHOLD_PERCENT
  }
}
