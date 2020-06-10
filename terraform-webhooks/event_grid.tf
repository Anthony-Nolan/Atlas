locals {
  // IMPORTANT: This matches the function name in DonorImportFunctions.cs. The values should remain in-sync at all times.
  import_donor_file_function_name = "ImportDonorFile"
}

resource "azurerm_eventgrid_event_subscription" "donor-file-upload" {
  name  = "${lower(var.ENVIRONMENT)}-donor-file-upload-subscription"
  scope = data.terraform_remote_state.atlas.outputs.storage_account.id

  subject_filter {
    subject_begins_with = "/blobServices/default/containers/${data.terraform_remote_state.atlas.outputs.donor_import.storage.donor_container_name}"
  }

  webhook_endpoint {
    url = "https://${data.terraform_remote_state.atlas.outputs.donor_import.function_app.hostname}/runtime/webhooks/EventGrid?functionName=${local.import_donor_file_function_name}&code=${var.DONOR_IMPORT_FUNCTION_MASTER_KEY}"
  }
}