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

variable "MATCHING_BATCH_SIZE" {
  type = number
}

variable "MAX_CONCURRENT_SERVICEBUS_FUNCTIONS" {
  type = number
}

variable "MAX_SCALE_OUT" {
  type = number
}

variable "RESULTS_BATCH_SIZE" {
  type = number
}