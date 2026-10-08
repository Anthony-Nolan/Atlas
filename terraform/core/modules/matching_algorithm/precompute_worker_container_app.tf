locals {
  precompute_worker_container_app_name = lower("${var.general.environment}-atlas-precompute-ca")
}

// The worker of the donor genotype precomputation (Data Refresh stage 65). It consumes the batch requests that the Data
// Refresh app publishes, and writes the results straight to SQL.
resource "azurerm_container_app" "atlas_precompute_worker" {
  name                         = local.precompute_worker_container_app_name
  container_app_environment_id = var.container_app_environment.id
  resource_group_name          = var.resource_group.name
  revision_mode                = "Single"
  tags                         = var.general.common_tags

  // aca_identity pulls from ACR and drives the Service Bus scale rule. The shared function apps identity is attached only
  // to read the Key Vault-backed secrets below - aca_identity is deliberately kept without secret-read rights (see
  // terraform/core/key_vault.tf). The role assignment for the shared identity lives in the root module, so it is ordered
  // by the depends_on on the module block.
  identity {
    type         = "UserAssigned"
    identity_ids = [var.aca_identity.id, var.key_vault.function_apps_identity_id]
  }

  template {
    // Always zero, deliberately not a variable. The worker is only needed during a data refresh, and it does not start
    // without an open refresh record (it cannot tell which database to write), so a warm replica would crash-loop
    // between refreshes. All replicas must also be gone before the next refresh, so that each starts against its target.
    min_replicas                = 0
    max_replicas                = var.PRECOMPUTE_WORKER_CONTAINER_MAX_REPLICAS
    polling_interval_in_seconds = var.PRECOMPUTE_WORKER_CONTAINER_SCALE_RULE_POLLING_INTERVAL_SECONDS

    custom_scale_rule {
      name             = "donor-genotype-precomputation-requests-scale-rule"
      custom_rule_type = "azure-servicebus"
      identity_id      = var.aca_identity.id
      metadata = {
        topicName        = azurerm_servicebus_topic.donor-genotype-precomputation-requests.name
        subscriptionName = azurerm_servicebus_subscription.precomputation-worker.name
        namespace        = var.servicebus_namespace.name
        messageCount     = tostring(var.PRECOMPUTE_WORKER_CONTAINER_SCALE_RULE_MESSAGE_COUNT)
      }
    }

    container {
      name   = "precompute-worker"
      image  = "${var.acr.login_server}/atlas-matching-precompute:${var.PRECOMPUTE_WORKER_CONTAINER_IMAGE_TAG}"
      cpu    = var.PRECOMPUTE_WORKER_CONTAINER_CPU
      memory = var.PRECOMPUTE_WORKER_CONTAINER_MEMORY

      env {
        name  = "ApplicationInsights__LogLevel"
        value = var.APPLICATION_INSIGHTS_LOG_LEVEL
      }
      // Container Apps do not set a cloud role name for Application Insights, unlike function apps.
      env {
        name  = "ApplicationInsights__CloudRoleName"
        value = local.precompute_worker_container_app_name
      }
      env {
        name        = "APPLICATIONINSIGHTS_CONNECTION_STRING"
        secret_name = "appinsights-connection-string"
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

      // The worker only receives and settles messages, so it needs no more than listen rights.
      env {
        name        = "MessagingServiceBus__ConnectionString"
        secret_name = "servicebus-read-only-connection-string"
      }

      env {
        name  = "PrecomputeWorker__RequestsTopic"
        value = azurerm_servicebus_topic.donor-genotype-precomputation-requests.name
      }
      env {
        name  = "PrecomputeWorker__RequestsSubscription"
        value = azurerm_servicebus_subscription.precomputation-worker.name
      }
      env {
        name  = "PrecomputeWorker__MaxConcurrentCalls"
        value = tostring(var.PRECOMPUTE_WORKER_MAX_CONCURRENT_CALLS)
      }
      env {
        name  = "PrecomputeWorker__PrefetchCount"
        value = tostring(var.PRECOMPUTE_WORKER_PREFETCH_COUNT)
      }
      env {
        name  = "PrecomputeWorker__MaxAutoLockRenewalMinutes"
        value = tostring(var.PRECOMPUTE_WORKER_MAX_AUTO_LOCK_RENEWAL_MINUTES)
      }
      env {
        name  = "PrecomputeWorker__BatchLeaseMinutes"
        value = tostring(var.PRECOMPUTE_WORKER_BATCH_LEASE_MINUTES)
      }
      env {
        name  = "PrecomputeWorker__MaxGroupFailuresPerBatch"
        value = tostring(var.PRECOMPUTE_WORKER_MAX_GROUP_FAILURES_PER_BATCH)
      }

      // The same variables as every other host that runs imputation, so that stored results match live ones.
      env {
        name  = "GenotypeImputation__MaximumExpandedGenotypesPerInput"
        value = tostring(var.MATCH_PREDICTION_MAX_EXPANDED_GENOTYPES_PER_INPUT)
      }
      env {
        name  = "HaplotypeFrequencySetCache__ActiveSetCacheExpiryMinutes"
        value = tostring(var.MATCH_PREDICTION_ACTIVE_HF_SET_CACHE_EXPIRY_MINUTES)
      }
      env {
        name  = "HaplotypeFrequencySetCache__MaxCachedFrequencySets"
        value = tostring(var.MATCH_PREDICTION_MAX_CACHED_FREQUENCY_SETS)
      }
      env {
        name  = "HaplotypeFrequencySetCache__SetCacheExpiryMinutes"
        value = tostring(var.MATCH_PREDICTION_SET_CACHE_EXPIRY_MINUTES)
      }

      env {
        name        = "ConnectionStrings__PersistentSql"
        secret_name = "matching-persistent-sql-connection-string"
      }
      env {
        name        = "ConnectionStrings__SqlA"
        secret_name = "matching-transient-a-sql-connection-string"
      }
      env {
        name        = "ConnectionStrings__SqlB"
        secret_name = "matching-transient-b-sql-connection-string"
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
        timeout          = 3
      }

      readiness_probe {
        path             = "/health/ready"
        port             = 8080
        transport        = "HTTP"
        initial_delay    = 10
        interval_seconds = 15
        timeout          = 5
      }
    }
  }

  // No ingress: the worker serves no traffic, and the health probes reach the container port without one.

  registry {
    server   = var.acr.login_server
    identity = var.aca_identity.id
  }

  secret {
    name  = "appinsights-connection-string"
    value = var.application_insights.connection_string
  }

  // Versioned secret IDs, matching the function apps' choice of a versioned SecretUri. Running replicas only pick up a
  // rotated value on a new revision or a restart.
  secret {
    name                = "azure-storage-connection-string"
    key_vault_secret_id = var.key_vault.secret_ids["azure-storage-connection-string"]
    identity            = var.key_vault.function_apps_identity_id
  }

  secret {
    name                = "servicebus-read-only-connection-string"
    key_vault_secret_id = var.key_vault.secret_ids["servicebus-read-only-connection-string"]
    identity            = var.key_vault.function_apps_identity_id
  }

  secret {
    name                = "matching-persistent-sql-connection-string"
    key_vault_secret_id = azurerm_key_vault_secret.matching_algorithm["matching-persistent-sql-connection-string"].id
    identity            = var.key_vault.function_apps_identity_id
  }

  secret {
    name                = "matching-transient-a-sql-connection-string"
    key_vault_secret_id = azurerm_key_vault_secret.matching_algorithm["matching-transient-a-sql-connection-string"].id
    identity            = var.key_vault.function_apps_identity_id
  }

  secret {
    name                = "matching-transient-b-sql-connection-string"
    key_vault_secret_id = azurerm_key_vault_secret.matching_algorithm["matching-transient-b-sql-connection-string"].id
    identity            = var.key_vault.function_apps_identity_id
  }

  secret {
    name                = "match-prediction-sql-connection-string"
    key_vault_secret_id = var.match_prediction_sql_database.connection_string_secret_id
    identity            = var.key_vault.function_apps_identity_id
  }
}
