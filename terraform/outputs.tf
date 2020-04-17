output "matching_algorithm" {
  value = {
    base_url = "https://${azurerm_function_app.atlas_matching_algorithm_function.default_hostname}"
    api_key  = var.APIKEY
  }
}