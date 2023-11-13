// Variables that allow for dependency inversion of other terraformed resources.

variable "app_service_plan" {
  type = object({
    id                  = string
    resource_group_name = string
  })
}

variable "application_insights" {
  type = object({
    instrumentation_key = string
  })
}

variable "azure_app_configuration" {
  type = object({
    id = string
    primary_read_key = list(object({
      connection_string = string
    }))
  })
}

variable "azure_storage" {
  type = object({
    id                        = string
    name                      = string
    primary_connection_string = string
  })
}

variable "donor_database_connection_string" {
  type = string
}

variable "mac_import_table" {
  type = object({
    name = string
  })
}

variable "matching_persistent_database_connection_string" {
  type = string
}

variable "matching_transient_a_database_connection_string" {
  type = string
}

variable "matching_transient_b_database_connection_string" {
  type = string
}

variable "original-search-matching-results-topic" {
  type = object({
    id   = string
    name = string
  })
}

variable "servicebus_namespace" {
  type = object({
    id   = string
    name = string
  })
}

variable "shared_function_storage" {
  type = object({
    primary_access_key = string
    name               = string
  })
}

variable "servicebus_namespace_authorization_rules" {
  type = object({
    read-write = object({ primary_connection_string = string })
    read-only  = object({ primary_connection_string = string })
    write-only = object({ primary_connection_string = string })
  })
}

variable "servicebus_topics" {
  type = object({
    alerts        = object({ name = string })
    notifications = object({ name = string })
  })
}

variable "sql_database" {
  type = object({
    name = string
  })
}

variable "sql_server" {
  type = object({
    id                          = string
    name                        = string
    fully_qualified_domain_name = string
  })
}