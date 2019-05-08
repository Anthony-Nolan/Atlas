variable SERVICE_PLAN_SKU {
  default = {
    tier = "Standard"
    size = "S1"
  }
}

variable DONORSERVICE_APIKEY {
  type = "string"
}

variable DONORSERVICE_BASEURL {
  type = "string"
}

variable HLASERVICE_APIKEY {
  type = "string"
}

variable HLASERVICE_BASEURL {
  type = "string"
}

variable APIKEY {
  type = "string"
}

variable CONNECTION_STRING_HANGFIRE {
  type = "string"
}

variable CONNECTION_STRING_SQL {
  type = "string"
}

variable CONNECTION_STRING_STORAGE {
  type = "string"
}
