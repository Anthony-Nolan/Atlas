// Variables set at release time.

variable "APPLICATION_INSIGHTS_LOG_LEVEL" {
  type = string
}

variable "DATABASE_PASSWORD" {
  type      = string
  sensitive = true
}

variable "DATABASE_USERNAME" {
  type = string
}

variable "IP_RESTRICTION_SETTINGS" {
  type    = list(string)
  default = []
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

variable "MAX_WAIT_FOR_ORIGINAL_RESULTS_MINUTES" {
  type = number
}

variable "ORIGINAL_RESULTS_WAIT_INTERVAL_SECONDS" {
  type = number
}

variable "REPEAT_SEARCH_RESULTS_READY_SUBSCRIPTION_NAMES" {
  type    = list(string)
  default = []
}

variable "RESULTS_BATCH_SIZE" {
  type = number
}

variable "RESULTS_DELETE_BATCH_SIZE" {
  type = number
}

variable "RESULTS_DELETE_CRON_SCHEDULE" {
  type = string
}

variable "RESULTS_DELETE_MAX_RUN_MINUTES" {
  type = number
}

variable "RESULTS_RETENTION_DAYS" {
  type = number
}

variable "SEARCH_RELATED_HLA_METADATA_CACHE_SLIDING_EXPIRATION_SEC" {
  type     = number
  nullable = true
}

variable "SERVICE_BUS_SEND_RETRY_COOLDOWN_SECONDS" {
  type = number
}

variable "SERVICE_BUS_SEND_RETRY_COUNT" {
  type = number
}

variable "STORE_ORIGINAL_SEARCH_RESULTS_BULKCOPY_BATCHSIZE" {
  type = number
}

variable "STORE_ORIGINAL_SEARCH_RESULTS_BULKCOPY_TIMEOUT" {
  type = number
}

variable "STORE_ORIGINAL_SEARCH_RESULTS_CHUNK_SIZE" {
  type = number
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
