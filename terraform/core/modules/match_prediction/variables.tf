variable "general" {
  type = object({
    environment = string
    location    = string
    common_tags = object({})
  })
}

variable "ip_restriction_settings" {
  type = list(object({
    ip_address = string
    subnet_mask = string
  }))
  description = "List of IP's to restrict function for. If none are provided the resources will only be available to other azure services."
}