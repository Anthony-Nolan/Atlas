// Variables that allow for dependency inversion of other terraformed resources.

variable "app_service_plan" {
  type = object({
    id                  = string
    resource_group_name = string
  })
}

variable "azure_storage" {
  type = object({
    name                      = string
    primary_connection_string = string
  })
}