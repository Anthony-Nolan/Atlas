variable "SERVICE_PLAN_SKU" {
  type    = object({
    tier = string,
    size = string
  })
  default = {
    tier = "Standard"
    size = "S1"
  }
}

variable "APIKEY" {
  type = string
}

variable "APPLICATION_INSIGHTS_LOG_LEVEL" {
  type    = string
  default = "Info"
}

variable "AZURE_CLIENT_ID" {
  type = string
}

variable "AZURE_CLIENT_SECRET" {
  type = string
}

variable "AZURE_STORAGE_SEARCH_RESULTS_BLOB_CONTAINER" {
  type    = string
  default = "search-algorithm-results"
}

variable "CONNECTION_STRING_SQL_A" {
  type = string
}

variable "CONNECTION_STRING_SQL_B" {
  type = string
}

variable "CONNECTION_STRING_SQL_PERSISTENT" {
  type = string
}

variable "CONNECTION_STRING_STORAGE" {
  type = string
}

variable "DATA_REFRESH_DB_SIZE_ACTIVE" {
  type    = string
  default = "S4"
}

variable "DATA_REFRESH_DB_SIZE_DORMANT" {
  type    = string
  default = "S0"
}

variable "DATA_REFRESH_DB_SIZE_REFRESH" {
  type    = string
  default = "S0"
}

variable "DATA_REFRESH_CRONTAB" {
  type    = string
  default = "0 0 0 * * Monday"
}

variable "DATA_REFRESH_DATABASE_A_NAME" {
  type = string
}

variable "DATA_REFRESH_DATABASE_B_NAME" {
  type = string
}

variable "DATA_REFRESH_DONOR_IMPORT_FUNCTION_NAME" {
  type = string
}

variable "DATABASE_OPERATITON_POLLING_INTERVAL_MILLISECONDS" {
  type    = string
  default = "1000"
}

variable "DATABASE_RESOURCE_GROUP" {
  type = string
}

variable "DATABASE_SERVER_NAME" {
  type = string
}

variable "DATABASE_SUBSCRIPTION_ID" {
  type = string
}

variable "DONORSERVICE_APIKEY" {
  type = string
}

variable "DONORSERVICE_BASEURL" {
  type = string
}

variable "FUNCTION_APP_SUBSCRIPTION_ID" {
  type = string
}

variable "MESSAGING_BUS_CONNECTION_STRING" {
  type = string
}

variable "MESSAGING_BUS_DONOR_BATCH_SIZE" {
  type = number
  default = 350
}

variable "MESSAGING_BUS_DONOR_CRON_SCHEDULE" {
  type = string
  default = "0 */1 * * * *"
}

variable "MESSAGING_BUS_DONOR_TOPIC" {
  type    = string
  default = "updated-searchable-donors"
}

variable "MESSAGING_BUS_DONOR_SUBSCRIPTION" {
  type    = string
  default = "searchalgorithm"
}

variable "MESSAGING_BUS_SEARCH_REQUESTS_QUEUE" {
  type    = string
  default = "search-algorithm-search-requests"
}

variable "MESSAGING_BUS_SEARCH_RESULTS_TOPIC" {
  type    = string
  default = "search-algorithm-results-notifications"
}

variable "NOTIFICATIONS_BUS_ALERTS_TOPIC" {
  type    = string
  default = "support-alerts"
}

variable "NOTIFICATIONS_BUS_CONNECTION_STRING" {
  type = string
}

variable "NOTIFICATIONS_BUS_NOTIFICATIONS_TOPIC" {
  type    = string
  default = "support-notifications"
}

variable "WMDA_FILE_URL" {
  type    = string
  default = "https://raw.githubusercontent.com/ANHIG/IMGTHLA/"
}

variable "WEBSITE_RUN_FROM_PACKAGE" {
  type    = string
  default = "1"
}