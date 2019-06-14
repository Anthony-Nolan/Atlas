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

variable "DONOR_SERVICE_API_KEY" {
  type = string
}

variable "DONOR_SERVICE_BASE_URL" {
  type = string
}

