// Variables set at release time.

variable "APPLICATION_INSIGHTS_LOG_LEVEL" {
  type = string
}

variable "DATABASE_PASSWORD" {
  type = string
}

variable "DATABASE_USERNAME" {
  type = string
}

variable "IP_RESTRICTION_SETTINGS" {
  type    = list(string)
  default = []
}

// External SQL variables
variable "use_external_sql" {
  type    = bool
  default = false
}

variable "external_sql_server_name" {
  type    = string
  default = ""
}

variable "external_sql_db_shared" {
  type    = string
  default = ""
}
