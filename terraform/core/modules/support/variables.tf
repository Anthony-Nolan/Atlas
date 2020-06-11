variable "default_servicebus_settings" {
  type = object({
    long-expiry                    = string
    audit-subscription-idle-delete = string
    default-read-lock              = string
    default-bus-size               = number
    default-message-retries        = number
  })
}