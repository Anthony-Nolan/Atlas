variable "AZURE_LOCATION" {
  type        = string
  description = "Azure region resources are deployed to."
  default     = "uksouth"
}

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
  type        = string
  description = "Name of the AD group used to control admin access to the SQL server."
}

variable "DATABASE_SERVER_AZUREAD_ADMINISTRATOR_OBJECTID" {
  type        = string
  description = "Object ID of admin access AD group."
}

variable "DATABASE_SERVER_AZUREAD_ADMINISTRATOR_TENANTID" {
  type        = string
  description = "ID of Tenant where admin access AD group resides."
}

variable "NAME_PREFIX" {
  type        = string
  description = "Prepended to resources that require globally unique names (storage accounts, etc)."
  default     = "Atlas"
}
