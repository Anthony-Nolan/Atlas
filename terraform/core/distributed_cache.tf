// Azure Managed Redis backing the distributed cache (Atlas.Common/Caching). One instance per environment: cache keys
// carry no environment prefix, so environments must never share one.
// See ArchitecturalDecisionRecord/Phase3/003-Redis_Hosting_AMR_vs_ACA.md for the hosting, sizing and network decisions.

locals {
  // The hostname "<name>.<region>.redis.azure.net" must be unique across ALL of Azure, so the name carries the same
  // "an-" (Anthony Nolan) prefix as the Key Vault. The longest environment gives "an-live-wmda-atlas-redis" (24).
  distributed_cache_name = lower("an-${local.environment}-atlas-redis")
}

resource "azurerm_managed_redis" "atlas" {
  name                = local.distributed_cache_name
  resource_group_name = azurerm_resource_group.atlas_resource_group.name
  location            = local.location
  sku_name            = var.DISTRIBUTED_CACHE_SKU_NAME
  tags                = local.common_tags

  // Changing this forces a new instance, so it has to be right for each environment when the instance is created.
  high_availability_enabled = var.DISTRIBUTED_CACHE_HIGH_AVAILABILITY_ENABLED

  // Atlas has no VNet, and AMR supports neither VNet injection nor IP firewall rules, so until a private endpoint exists
  // the cache is reached over its public endpoint, protected by TLS and Entra ID. See "Network posture" in the ADR.
  public_network_access = "Enabled"

  default_database {
    // Entra ID only: the apps sign in with function_apps_identity, so there is no access key to hold as a secret.
    access_keys_authentication_enabled = false
    client_protocol                    = "Encrypted"
    clustering_policy                  = "OSSCluster"

    // The default, VolatileLRU, only evicts keys that have a TTL, so a full cache holding any key without one rejects
    // writes instead of shedding cold data. Every value has a source of truth in SQL or Table Storage, so any key may go.
    eviction_policy = "AllKeysLRU"

    // No persistence_* arguments: every value can be rebuilt from its source, and the apps already work without the cache.
  }
}

resource "azurerm_managed_redis_access_policy_assignment" "function_apps_identity" {
  managed_redis_id = azurerm_managed_redis.atlas.id
  object_id        = azurerm_user_assigned_identity.function_apps_identity.principal_id
}

// Records who connects and signs in to the public endpoint, in the REDConnectionEvents table.
resource "azurerm_monitor_diagnostic_setting" "distributed_cache_connection_events" {
  name                           = "connection-events"
  target_resource_id             = azurerm_managed_redis.atlas.default_database[0].id
  log_analytics_workspace_id     = azurerm_log_analytics_workspace.ai_workspace.id
  log_analytics_destination_type = "Dedicated"

  enabled_log {
    category = "ConnectionEvents"
  }
}

locals {
  // A plain app setting, host:port only, as the apps sign in with Microsoft Entra ID rather than a key: they set
  // DistributedCache:ManagedIdentityClientId to the client id of function_apps_identity, which has the access policy
  // above, and Atlas.Common's RedisConnection then turns on TLS and Entra sign-in. An empty connection string would turn
  // the cache off.
  distributed_cache_handoff = {
    connection_string = "${azurerm_managed_redis.atlas.hostname}:${azurerm_managed_redis.atlas.default_database[0].port}"
  }
}
