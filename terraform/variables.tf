variable "APIKEY" {
  type = string
}

variable "APPLICATION_INSIGHTS_LOG_LEVEL" {
  type    = string
  default = "Info"
}

variable "ATLAS_DATABASE_SERVER_ADMIN_LOGIN" {
  type    = string
  default = "atlas-admin"
}

variable "ATLAS_DATABASE_SERVER_LOGIN_PASSWORD" {
  type = string
}

variable "ATLAS_MATCHING_DATABASE_USERNAME" {
  type    = string
  default = "atlas-matching"
}

variable "ATLAS_MATCHING_DATABASE_PASSWORD" {
  type = string
}

variable "AZURE_CLIENT_ID" {
  type = string
}

variable "AZURE_CLIENT_SECRET" {
  type = string
}

variable "AZURE_STORAGE_TIER" {
  type    = string
  default = "Standard"
}

variable "AZURE_STORAGE_REPLICATION_TYPE" {
  type    = string
  default = "LRS"
}

variable "DATA_REFRESH_CRONTAB" {
  type    = string
  default = "0 0 0 * * Monday"
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

variable "DATA_REFRESH_DONOR_IMPORT_FUNCTION_NAME" {
  type = string
}

variable "DATABASE_OPERATITON_POLLING_INTERVAL_MILLISECONDS" {
  type    = string
  default = "1000"
}

variable "DONOR_SERVICE_APIKEY" {
  type = string
}

variable "DONOR_SERVICE_BASEURL" {
  type = string
}

variable "ENVIRONMENT" {
  type = string
}

variable "FUNCTION_STORAGE_REPLICATION_TYPE" {
  type    = string
  default = "LRS"
}

variable "FUNCTION_STORAGE_TIER" {
  type    = string
  default = "Standard"
}

variable "HLA_SERVICE_APIKEY" {
  type = string
}

variable "HLA_SERVICE_BASEURL" {
  type = string
}

variable "LOCATION" {
  type    = string
  default = "uksouth"
}

variable "MESSAGING_BUS_DONOR_BATCH_SIZE" {
  type    = number
  default = 350
}

variable "MESSAGING_BUS_DONOR_CRON_SCHEDULE" {
  type    = string
  default = "0 */1 * * * *"
}

variable "SERVICE_PLAN_SKU" {
  type = object({
    tier = string,
    size = string
  })
  default = {
    tier = "Standard"
    size = "S1"
  }
}

variable "WMDA_FILE_URL" {
  type    = string
  default = "https://raw.githubusercontent.com/ANHIG/IMGTHLA/"
}

variable "WEBSITE_RUN_FROM_PACKAGE" {
  type    = string
  default = "1"
}