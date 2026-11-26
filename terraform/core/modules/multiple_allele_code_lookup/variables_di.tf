// Variables that allow for dependency inversion of other terraformed resources.

variable "azure_storage" {
  type = object({
    id                        = string
    primary_connection_string = string
  })
}