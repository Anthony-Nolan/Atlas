output "search_algorithm_service" {
  value = {
    base_url = azurerm_app_service.search_algorithm.default_site_hostname
    api_key  = var.APIKEY
  }
}
