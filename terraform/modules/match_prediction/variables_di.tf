// Variables that allow for dependency inversion of other terraformed resources.

variable "app_service_plan" {
  type = object({
    id                  = string
    resource_group_name = string
  })
}

variable "shared_function_storage" {
  type = object({
    primary_connection_string = string
  })
}

variable "azure_storage" {
  type = object({
    name                      = string
    primary_connection_string = string
  })
}

variable "application_insights" {
  type = object({
    instrumentation_key = string
  })
}