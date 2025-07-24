variable "general" {
  type = object({
    environment = string
    location    = string
    common_tags = object({})
  })
}

variable "default_servicebus_settings" {
  type = object({
    long-expiry                         = string
    audit-subscription-short-ttl-expiry = string
    audit-subscription-ttl-expiry       = string
    debug-subscription-ttl-expiry       = string
    default-read-lock                   = string
    default-bus-size                    = number
    default-message-retries             = number
  })
}