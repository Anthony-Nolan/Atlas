output "search_algorithm_service" {
  value = {
    base_url = "https://${azurerm_function_app.search_algorithm_function.default_hostname}}"
    api_key  = var.APIKEY
  }
}
