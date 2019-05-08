variable service-plan-sku {
  default = {
    tier = "Standard"
    size = "S1"
  }
}

variable donorservice_apiKey {
  type = "string"
}

variable donorservice_baseUrl {
  type = "string"
}

variable hlaservice_apiKey {
  type = "string"
}

variable hlaservice_baseUrl {
  type = "string"
}

variable apiKey {
  type = "string"
}

variable connection_string_hangfire {
  type = "string"
}

variable connection_string_sql {
  type = "string"
}

variable connection_string_storage {
  type = "string"
}
