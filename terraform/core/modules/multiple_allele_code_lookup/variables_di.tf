// Variables that allow for dependency inversion of other terraformed resources.

variable "azure_storage" {
  type = object({
    name                      = string
    primary_connection_string = string
  })
}