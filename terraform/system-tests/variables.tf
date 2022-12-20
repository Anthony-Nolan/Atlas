variable "AZURE_SUBSCRIPTION_ID" {
  type        = string
  description = "ID of the Azure subscription into which the system will be deployed."
}

variable "DATABASE_SERVER_ADMIN_LOGIN" {
  type    = string
  default = "atlas-admin"
}

variable "DATABASE_SERVER_ADMIN_LOGIN_PASSWORD" {
  type = string
}

variable "DATABASE_SERVER_AZUREAD_ADMINISTRATOR_LOGIN_USERNAME" {
  type = string
}

variable "DATABASE_SERVER_AZUREAD_ADMINISTRATOR_OBJECTID" {
  type = string
}

variable "DATABASE_SERVER_AZUREAD_ADMINISTRATOR_TENANTID" {
  type = string
}