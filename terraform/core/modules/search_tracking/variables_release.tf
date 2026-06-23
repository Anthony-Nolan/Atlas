// Variables set at release time.

variable "APPLICATION_INSIGHTS_LOG_LEVEL" {
  type = string
}

variable "AUTOMAPPER_LICENSE_KEY" {
  type      = string
  sensitive = true
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
variable "USE_EXTERNAL_SQL" {
  type    = bool
  default = false
}

variable "EXTERNAL_SQL_SERVER_NAME" {
  type    = string
  default = ""
}

variable "EXTERNAL_SQL_DB_SHARED" {
  type    = string
  default = ""
}
