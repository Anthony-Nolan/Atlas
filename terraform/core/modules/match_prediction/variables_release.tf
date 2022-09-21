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

variable "MESSAGING_BUS_MATCH_PREDICTION_BATCH_SIZE" {
  type = number
}

variable "MESSAGING_BUS_MATCH_PREDICTION_CRON_SCHEDULE" {
  type = string
}

variable "SUPPRESS_COMPRESSED_PHENOTYPE_CONVERSION_EXCEPTIONS" {
  type = bool
}

variable "WEBSITE_RUN_FROM_PACKAGE" {
  type = string
}