resource "azurerm_storage_container" "donor_blob_storage" {
  name                  = "donors"
  storage_account_name  = var.azure_storage.name
  container_access_type = "private"
}

locals {
  donor_id_checker_results_container_name   = "donor-id-checker/results"
  donor_info_checker_results_container_name = "donor-info-checker/results"
}
