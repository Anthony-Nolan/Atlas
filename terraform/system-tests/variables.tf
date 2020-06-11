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