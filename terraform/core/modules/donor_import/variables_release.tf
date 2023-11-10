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

variable "DELETE_PUBLISHED_DONOR_UPDATES_CRONTAB" {
  type = string
}

variable "IP_RESTRICTION_SETTINGS" {
  type    = list(string)
  default = []
}

variable "MAX_INSTANCES" {
  type = number
}

variable "NOTIFICATIONS_ON_DELETION_OF_INVALID_DONOR" {
  type = bool
}

variable "PUBLISH_DONOR_UPDATES_CRONTAB" {
  type = string
}

variable "PUBLISHED_UPDATE_EXPIRY_IN_DAYS" {
  type = number
}

variable "STALLED_FILE_CHECK_CRONTAB" {
  type = string
}

variable "STALLED_FILE_DURATION" {
  type = string
}

variable "FAILURE_LOGS_CRONTAB" {
  type = string
}

variable "FAILURE_LOGS_EXPIRY_IN_DAYS" {
  type = number
}