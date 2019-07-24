variable "SERVICE_PLAN_SKU" {
  default = {
    tier = "Standard"
    size = "S1"
  }
}

variable "APIKEY" {
  type = string
}

variable "APPLICATION_INSIGHTS_LOG_LEVEL" {
  type = string
  default = "Info"
}

variable "AZURE_STORAGE_SEARCH_RESULTS_BLOB_CONTAINER" {
  type = string
  default = "search-algorithm-results"
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

variable "MESSAGING_BUS_CONNECTION_STRING" {
  type = string
}

variable "MESSAGING_BUS_DONOR_TOPIC" {
  type = string
  default = "updated-searchable-donors"
}

variable "MESSAGING_BUS_DONOR_SUBSCRIPTION" {
  type = string
  default = "searchalgorithm"
}

variable "MESSAGING_BUS_SEARCH_REQUESTS_QUEUE" {
  type = string
  default = "search-algorithm-search-requests"
}

variable "MESSAGING_BUS_SEARCH_RESULTS_TOPIC" {
  type = string
  default = "search-algorithm-results-notifications"
}

variable "WMDA_HLA_DATABASE_VERSION" {
  type = string
  default = "3330"
}

variable "WMDA_FILE_URL" {
  type = string
  default = "https://raw.githubusercontent.com/ANHIG/IMGTHLA/"
}