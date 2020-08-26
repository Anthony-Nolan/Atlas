locals {
  // IMPORTANT: This matches the function name in DonorImportFunctions.cs. The values should remain in-sync at all times.
  import_donor_file_function_name = "ImportDonorFile"
  // IMPORTANT: This matches the function name in HaplotypeFrequencySetFunctions.cs. The values should remain in-sync at all times.
  import_haplotype_frequency_set_function_name = "ImportHaplotypeFrequencySet"
}

module "donor_import_function_key" {
  source          = "./modules/fetch_key"
  function_app_id = data.terraform_remote_state.atlas.outputs.donor_import.function_app.id
  client_id       = var.AZURE_CLIENT_ID
  client_secret   = var.AZURE_CLIENT_SECRET
}

module "match_prediction_function_key" {
  source          = "./modules/fetch_key"
  function_app_id = data.terraform_remote_state.atlas.outputs.match_prediction.function_app.id
  client_id       = var.AZURE_CLIENT_ID
  client_secret   = var.AZURE_CLIENT_SECRET
}

resource "azurerm_eventgrid_event_subscription" "haplotype-frequency-set-upload" {
  name                 = "${lower(var.ENVIRONMENT)}-haplotype-frequency-set-upload-subscription"
  scope                = data.terraform_remote_state.atlas.outputs.storage_account.id
  included_event_types = ["Microsoft.Storage.BlobCreated"]

  subject_filter {
    subject_begins_with = "/blobServices/default/containers/${data.terraform_remote_state.atlas.outputs.match_prediction.storage.haplotype_frequency_set_container_name}"
  }

  webhook_endpoint {
    url = "https://${data.terraform_remote_state.atlas.outputs.match_prediction.function_app.hostname}/runtime/webhooks/EventGrid?functionName=${local.import_haplotype_frequency_set_function_name}&code=${module.match_prediction_function_key.function_key.key}"
  }
}