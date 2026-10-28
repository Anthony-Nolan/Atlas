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

// The identity of the Atlas apps (identity.tf in the root module), with which they resolve Key Vault references and
// sign in to other resources with Microsoft Entra ID.
variable "function_apps_identity" {
  type = object({
    id           = string
    client_id    = string
    principal_id = string
  })
}
