// Variables that allow for dependency inversion of other terraformed resources.

variable "resource_group" {
  type = object({
    name = string
  })
}

variable "servicebus_namespace" {
  type = object({
    id   = string
    name = string
  })
}

variable "service_bus_topics_with_alerts" {
  type = map(object({
    name = string
  }))
}