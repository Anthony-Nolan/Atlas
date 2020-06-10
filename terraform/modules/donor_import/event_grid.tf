locals {
  // IMPORTANT: This matches the function name in DonorImportFunctions.cs. The values should remain in-sync at all times.
  import_donor_file_function_name = "ImportDonorFile"
}

resource "azurerm_eventgrid_event_subscription" "donor-file-upload" {
  name  = "${var.general.environment}-donor-file-upload-subscription"
  scope = var.resource_group.id

  subject_filter {
    subject_begins_with = azurerm_storage_container.donor_blob_storage.name
  }

  webhook_endpoint {
    url = "https://${azurerm_function_app.atlas_donor_import_function.default_hostname}/runtime/webhooks/EventGrid?functionName=${local.import_donor_file_function_name}&code=${var.FUNCTIONS_MASTER_KEY}"
  }
}