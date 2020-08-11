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
}