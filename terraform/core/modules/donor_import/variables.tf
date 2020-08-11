variable "general" {
  type = object({
    environment = string
    location    = string
    common_tags = object({})
  })
}

variable "default_servicebus_settings" {
  type = object({
    long-expiry                    = string
    audit-subscription-idle-delete = string
    default-read-lock              = string
    default-bus-size               = number
    default-message-retries        = number
  })
}

variable "ip_restriction_settings" {
  type = list(object({
    ip_address = string
    subnet_mask = string
  }))
}