locals {
  atlas_match_prediction_container_app_name = lower("${var.general.environment}-atlas-match-prediction-ca")
}

resource "azurerm_container_app" "atlas_match_prediction" {
  name                         = local.atlas_match_prediction_container_app_name
  container_app_environment_id = var.container_app_environment.id
  resource_group_name          = var.app_service_plan.resource_group_name
  revision_mode                = "Single"
  tags                         = var.general.common_tags

  identity {
    type         = "UserAssigned"
    identity_ids = [var.acr_pull_identity.id]
  }

  template {
    min_replicas = var.CONTAINER_MIN_REPLICAS
    max_replicas = var.CONTAINER_MAX_REPLICAS

    container {
      name   = "match-prediction"
      image  = "${var.acr.login_server}/atlas-match-prediction:${var.CONTAINER_IMAGE_TAG}"
      cpu    = var.CONTAINER_CPU
      memory = var.CONTAINER_MEMORY

      // Application settings mirrored from function app
      env {
        name  = "ApplicationInsights__LogLevel"
        value = var.APPLICATION_INSIGHTS_LOG_LEVEL
      }
      env {
        name        = "APPLICATIONINSIGHTS_CONNECTION_STRING"
        secret_name = "appinsights-connection-string"
      }

      env {
        name        = "AzureStorage__ConnectionString"
        secret_name = "azure-storage-connection-string"
      }
      env {
        name        = "AzureStorage__MatchPredictionConnectionString"
        secret_name = "azure-storage-connection-string"
      }
      env {
        name  = "AzureStorage__MatchPredictionResultsBlobContainer"
        value = azurerm_storage_container.match_prediction_results_container.name
      }
      env {
        name  = "AzureStorage__MatchPredictionRequestsBlobContainer"
        value = azurerm_storage_container.match_prediction_requests_container.name
      }

      env {
        name        = "HlaMetadataDictionary__AzureStorageConnectionString"
        secret_name = "azure-storage-connection-string"
      }
      env {
        name  = "HlaMetadataDictionary__SearchRelatedMetadata__CacheSlidingExpirationInSeconds"
        value = var.SEARCH_RELATED_HLA_METADATA_CACHE_SLIDING_EXPIRATION_SEC != null ? tostring(var.SEARCH_RELATED_HLA_METADATA_CACHE_SLIDING_EXPIRATION_SEC) : ""
      }

      env {
        name        = "MacDictionary__AzureStorageConnectionString"
        secret_name = "azure-storage-connection-string"
      }
      env {
        name  = "MacDictionary__TableName"
        value = var.mac_import_table.name
      }

      env {
        name        = "MessagingServiceBus__ConnectionString"
        secret_name = "servicebus-manage-connection-string"
      }

      env {
        name  = "MessagingServiceBus__SendRetryCount"
        value = tostring(var.SERVICE_BUS_SEND_RETRY_COUNT)
      }

      env {
        name  = "MessagingServiceBus__SendRetryCooldownSeconds"
        value = tostring(var.SERVICE_BUS_SEND_RETRY_COOLDOWN_SECONDS)
      }

      env {
        name  = "MatchPredictionWorker__RequestsSubscription"
        value = azurerm_servicebus_subscription.parallel-match-prediction-request-runner.name
      }
      env {
        name  = "MatchPredictionRequests__RequestsTopic"
        value = azurerm_servicebus_topic.parallel-match-prediction-requests.name
      }
      env {
        name  = "MatchPredictionRequests__ResultsTopic"
        value = azurerm_servicebus_topic.match-prediction-results.name
      }
      env {
        name  = "MatchPredictionRequests__MaxParallelism"
        value = tostring(var.MATCH_PREDICTION_REQUESTS_MAX_PARALLELISM)
      }
      env {
        name  = "MatchPredictionWorker__BatchSize"
        value = tostring(var.MATCH_PREDICTION_WORKER_BATCH_SIZE)
      }

      env {
        name        = "SearchTrackingServiceBus__ConnectionString"
        secret_name = "servicebus-write-only-connection-string"
      }
      env {
        name  = "SearchTrackingServiceBus__SearchTrackingTopic"
        value = var.servicebus_topics.search_tracking.name
      }
      env {
        name  = "SearchTrackingServiceBus__SendRetryCount"
        value = tostring(var.SERVICE_BUS_SEND_RETRY_COUNT)
      }
      env {
        name  = "SearchTrackingServiceBus__SendRetryCooldownSeconds"
        value = tostring(var.SERVICE_BUS_SEND_RETRY_COOLDOWN_SECONDS)
      }

      env {
        name        = "NotificationsServiceBus__ConnectionString"
        secret_name = "servicebus-write-only-connection-string"
      }
      env {
        name  = "NotificationsServiceBus__AlertsTopic"
        value = var.servicebus_topics.alerts.name
      }
      env {
        name  = "NotificationsServiceBus__NotificationsTopic"
        value = var.servicebus_topics.notifications.name
      }
      env {
        name  = "NotificationsServiceBus__SendRetryCount"
        value = tostring(var.SERVICE_BUS_SEND_RETRY_COUNT)
      }
      env {
        name  = "NotificationsServiceBus__SendRetryCooldownSeconds"
        value = tostring(var.SERVICE_BUS_SEND_RETRY_COOLDOWN_SECONDS)
      }

      env {
        name        = "ConnectionStrings__MatchPredictionSql"
        secret_name = "match-prediction-sql-connection-string"
      }

      liveness_probe {
        path             = "/health/live"
        port             = 8080
        transport        = "HTTP"
        initial_delay    = 10
        interval_seconds = 30
      }

      readiness_probe {
        path             = "/health/ready"
        port             = 8080
        transport        = "HTTP"
        interval_seconds = 10
      }
    }
  }

  ingress {
    external_enabled = true
    target_port      = 8080
    transport        = "http"

    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  registry {
    server   = var.acr.login_server
    identity = var.acr_pull_identity.id
  }

  secret {
    name  = "appinsights-connection-string"
    value = var.application_insights.connection_string
  }

  secret {
    name  = "azure-storage-connection-string"
    value = var.azure_storage.primary_connection_string
  }

  secret {
    name  = "servicebus-manage-connection-string"
    value = var.servicebus_namespace_authorization_rules.manage.primary_connection_string
  }

  secret {
    name  = "servicebus-write-only-connection-string"
    value = var.servicebus_namespace_authorization_rules.write-only.primary_connection_string
  }

  secret {
    name  = "match-prediction-sql-connection-string"
    value = local.match_prediction_database_connection_string
  }
}
