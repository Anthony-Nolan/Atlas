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
  type = list(object({
    ip_address = string
    subnet_id = string
  }))
}

variable "MAC_SOURCE" {
  type = string
}

variable "WEBSITE_RUN_FROM_PACKAGE" {
  type = string
}
