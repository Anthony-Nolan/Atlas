// Variables set at release time.

variable "APPLICATION_INSIGHTS_LOG_LEVEL" {
  type = string
}

variable "AZURE_CLIENT_ID" {
  type = string
}

variable "AZURE_CLIENT_SECRET" {
  type = string
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

variable "DATABASE_OPERATION_POLLING_INTERVAL_MILLISECONDS" {
  type = string
}

variable "DATABASE_PASSWORD" {
  type = string
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

variable "FUNCTION_HOST_KEY" {
  type = string
}

variable "IP_RESTRICTION_SETTINGS" {
  type = list(object({
    ip_address                = string
    virtual_network_subnet_id = string
    name                      = string
    priority                  = number
    action                    = string
  }))
}

variable "MESSAGING_BUS_DONOR_BATCH_SIZE" {
  type = number
}

variable "MESSAGING_BUS_DONOR_CRON_SCHEDULE" {
  type = string
}

variable "WEBSITE_RUN_FROM_PACKAGE" {
  type = string
}

variable "WMDA_FILE_URL" {
  type = string
}
