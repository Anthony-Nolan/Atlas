locals {
  // IMPORTANT: This matches the function name in DonorImportFunctions.cs. The values should remain in-sync at all times.
  import_donor_file_function_name = "ImportDonorFile"
  // IMPORTANT: This matches the function name in HaplotypeFrequencySetFunctions.cs. The values should remain in-sync at all times.
  import_haplotype_frequency_set_function_name = "ImportHaplotypeFrequencySet"
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

resource "azurerm_eventgrid_event_subscription" "haplotype-frequency-set-upload" {
  name  = "${lower(var.ENVIRONMENT)}-haplotype-frequency-set-upload-subscription"
  scope = data.terraform_remote_state.atlas.outputs.storage_account.id

  subject_filter {
    subject_begins_with = "/blobServices/default/containers/${data.terraform_remote_state.atlas.outputs.match_prediction.storage.haplotype_frequency_set_container_name}"
  }

  webhook_endpoint {
    url = "https://${data.terraform_remote_state.atlas.outputs.match_prediction.function_app.hostname}/runtime/webhooks/EventGrid?functionName=${local.import_haplotype_frequency_set_function_name}&code=${var.MATCH_PREDICTION_FUNCTION_MASTER_KEY}"
  }
}