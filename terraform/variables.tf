variable "SERVICE_PLAN_SKU" {
  default = {
    tier = "Standard"
    size = "S1"
  }
}

variable "APIKEY" {
  type = string
}

variable "CONNECTION_STRING_SQL_A" {
  type = string
}

variable "CONNECTION_STRING_SQL_B" {
  type = string
}

variable "CONNECTION_STRING_SQL_PERSISTENT" {
  type = string
}

variable "CONNECTION_STRING_STORAGE" {
  type = string
}

variable "APPLICATION_INSIGHTS_LOG_LEVEL" {
  type = string
  default = "Info"
}

variable "WMDA_HLA_DATABASE_VERSION" {
  type = string
  default = "3330"
}

variable "WMDA_FILE_URL" {
  type = string
  default = "https://raw.githubusercontent.com/ANHIG/IMGTHLA/"
}