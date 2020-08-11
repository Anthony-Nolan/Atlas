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
    ip_address                = string
    virtual_network_subnet_id = string
    name                      = string
    priority                  = number
    action                    = string
  }))
}