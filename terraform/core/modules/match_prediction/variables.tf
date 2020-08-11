variable "general" {
  type = object({
    environment = string
    location    = string
    common_tags = object({})
  })
}