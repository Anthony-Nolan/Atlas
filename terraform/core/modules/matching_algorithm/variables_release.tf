// Variables set at release time.

variable "APPLICATION_INSIGHTS_LOG_LEVEL" {
  type = string
}

variable "AUTOMAPPER_LICENSE_KEY" {
  type      = string
  sensitive = true
}

variable "AZURE_CLIENT_ID" {
  type = string
}

variable "AZURE_OAUTH_BASEURL" {
  type = string
}

variable "AZURE_TENANT_ID" {
  type = string
}

variable "DATA_REFRESH_AUTO_RUN" {
  type = bool
}

variable "DATA_REFRESH_DB_AUTO_PAUSE_ACTIVE" {
  type = number
}

variable "DATA_REFRESH_DB_AUTO_PAUSE_DORMANT" {
  type = number
}

variable "DATA_REFRESH_DB_SIZE_ACTIVE" {
  type = string
}

variable "DATA_REFRESH_DB_SIZE_DORMANT" {
  type = string
}

variable "DATA_REFRESH_DB_SIZE_REFRESH" {
  type = string
}

variable "DATA_REFRESH_CRONTAB" {
  type = string
}

variable "DATABASE_MAX_SIZE_GB" {
  type = string
}

variable "DATABASE_OPERATION_POLLING_INTERVAL_MILLISECONDS" {
  type = string
}

variable "DATABASE_PASSWORD" {
  type = string
}

variable "DATABASE_TRANSIENT_TIMEOUT" {
  type = number
}

variable "DATABASE_USERNAME" {
  type = string
}

variable "DONOR_IMPORT_DATABASE_PASSWORD" {
  type = string
}

variable "DONOR_IMPORT_DATABASE_USERNAME" {
  type = string
}

variable "DONOR_WRITE_TRANSACTIONALITY__DATA_REFRESH" {
  type        = bool
  default     = false
  description = "Should the Write for a Donor be entirely Transactional when running DataRefresh. 'false' for greater performance. 'true' for greater reliability"
}

variable "DONOR_WRITE_TRANSACTIONALITY__DONOR_UPDATES" {
  type        = bool
  default     = true
  description = "Should the Write for a Donor be entirely Transactional when running DataRefresh. 'false' for greater performance. 'true' for greater reliability"
}

variable "DATA_REFRESH_LEASE_DURATION_MINUTES" {
  type = number
}

variable "DATA_REFRESH_LEASE_RENEWAL_INTERVAL_SECONDS" {
  type = number
}

variable "DATA_REFRESH_WATCHDOG_CRON_SCHEDULE" {
  type = string
}

variable "DATA_REFRESH_WATCHDOG_GRACE_DURATION_MINUTES" {
  type = number
}

variable "DATA_REFRESH_PRECOMPUTE_ABANDON_BATCHES_CRON_SCHEDULE" {
  type = string
}

variable "DATA_REFRESH_PRECOMPUTE_FINALISE_RUNS_CRON_SCHEDULE" {
  type = string
}

variable "DATA_REFRESH_PRECOMPUTE_MAX_BATCH_RETRIES" {
  type = number
}

variable "DATA_REFRESH_PRECOMPUTE_REQUEUE_BATCHES_CRON_SCHEDULE" {
  type = string
}

variable "IP_RESTRICTION_SETTINGS" {
  type    = list(string)
  default = []
}

variable "MAINTENANCE_GCCOLLECT_DISABLED" {
  type = bool
}

variable "MAINTENANCE_GCCOLLECT_CRON_SCHEDULE" {
  type = string
}

variable "MATCH_PREDICTION_ACTIVE_HF_SET_CACHE_EXPIRY_MINUTES" {
  type = number
}

variable "MATCH_PREDICTION_MAX_CACHED_FREQUENCY_SETS" {
  type = number
}

variable "MATCH_PREDICTION_MAX_EXPANDED_GENOTYPES_PER_INPUT" {
  type = number
}

variable "MATCH_PREDICTION_SET_CACHE_EXPIRY_MINUTES" {
  type = number
}

variable "MATCHING_BATCH_SIZE" {
  type = number
}

variable "MAX_CONCURRENT_SERVICEBUS_FUNCTIONS" {
  type = number
}

variable "MAX_SCALE_OUT" {
  type = number
}

variable "MESSAGING_BUS_DONOR_BATCH_SIZE" {
  type = number
}

variable "MESSAGING_BUS_DONOR_CRON_SCHEDULE" {
  type = string
}

variable "SERVICE_BUS_SEND_RETRY_COOLDOWN_SECONDS" {
  type = number
}

variable "SERVICE_BUS_SEND_RETRY_COUNT" {
  type = number
}

variable "WEBSITE_RUN_FROM_PACKAGE" {
  type = string
}

variable "WMDA_FILE_URL" {
  type = string
}

variable "RESULTS_BATCH_SIZE" {
  type = number
}

variable "SEARCH_RELATED_HLA_METADATA_CACHE_SLIDING_EXPIRATION_SEC" {
  type     = number
  nullable = true
}

// Precompute worker Container App release variables

variable "PRECOMPUTE_WORKER_CONTAINER_IMAGE_TAG" {
  type    = string
  default = "latest"
}

variable "PRECOMPUTE_WORKER_CONTAINER_CPU" {
  type    = number
  default = 2.0
}

variable "PRECOMPUTE_WORKER_CONTAINER_MEMORY" {
  type    = string
  default = "4Gi"
}

variable "PRECOMPUTE_WORKER_CONTAINER_MAX_REPLICAS" {
  type    = number
  default = 10
}

variable "PRECOMPUTE_WORKER_CONTAINER_SCALE_RULE_MESSAGE_COUNT" {
  type    = number
  default = 1
}

variable "PRECOMPUTE_WORKER_CONTAINER_SCALE_RULE_POLLING_INTERVAL_SECONDS" {
  type    = number
  default = 30
}

variable "PRECOMPUTE_WORKER_MAX_CONCURRENT_CALLS" {
  type    = number
  default = 1
}

variable "PRECOMPUTE_WORKER_PREFETCH_COUNT" {
  type    = number
  default = 0
}

variable "PRECOMPUTE_WORKER_MAX_AUTO_LOCK_RENEWAL_MINUTES" {
  type    = number
  default = 60
}

variable "PRECOMPUTE_WORKER_BATCH_LEASE_MINUTES" {
  type    = number
  default = 60
}

variable "PRECOMPUTE_WORKER_MAX_GROUP_FAILURES_PER_BATCH" {
  type    = number
  default = 100
}

// External SQL variables
variable "USE_EXTERNAL_SQL" {
  type    = bool
  default = false
}

variable "EXTERNAL_SQL_SERVER_NAME" {
  type    = string
  default = ""
}

variable "EXTERNAL_SQL_DB_SHARED" {
  type    = string
  default = ""
}

variable "EXTERNAL_SQL_DB_MATCHING_A" {
  type    = string
  default = ""
}

variable "EXTERNAL_SQL_DB_MATCHING_B" {
  type    = string
  default = ""
}

variable "EXTERNAL_SQL_RESOURCE_GROUP" {
  type    = string
  default = ""
}

variable "EXTERNAL_SQL_SUBSCRIPTION_ID" {
  type    = string
  default = ""
}
