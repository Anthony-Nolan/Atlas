// Variables set at release time.

variable "APPLICATION_INSIGHTS_LOG_LEVEL" {
  type = string
}

variable "DATABASE_PASSWORD" {
  type = string
}

variable "DATABASE_USERNAME" {
  type = string
}

variable "IP_RESTRICTION_SETTINGS" {
  type    = list(string)
  default = []
}

variable "MAC_SOURCE" {
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

variable "SEARCH_RELATED_HLA_METADATA_CACHE_SLIDING_EXPIRATION_SEC" {
  type     = number
  nullable = true
}