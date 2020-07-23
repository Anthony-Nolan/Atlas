locals {
  // IMPORTANT: This matches the function name in DonorImportFunctions.cs. The values should remain in-sync at all times.
  import_donor_file_function_name = "ImportDonorFile"
  // IMPORTANT: This matches the function name in HaplotypeFrequencySetFunctions.cs. The values should remain in-sync at all times.
  import_haplotype_frequency_set_function_name = "ImportHaplotypeFrequencySet"
}

module "donor_import_function_key" {
  source = "github.com/eltimmo/terraform-azure-function-app-get-keys"
  resource_group_name  = var.TERRAFORM_RESOURCE_GROUP_NAME
  function_app_id = "/subscriptions/6114522f-eea5-44ab-94ab-af37ffffc4d3/resourceGroups/DEV-ATLAS-RESOURCE-GROUP/providers/Microsoft.Web/sites/DEV-ATLAS-DONOR-IMPORT-FUNCTION"
  key_set = "masterKey"
  key = "masterKey"
}

module "match_prediction_function_key" {
  source = "github.com/eltimmo/terraform-azure-function-app-get-keys"
  resource_group_name  = var.TERRAFORM_RESOURCE_GROUP_NAME
  function_app_id = "/subscriptions/6114522f-eea5-44ab-94ab-af37ffffc4d3/resourceGroups/DEV-ATLAS-RESOURCE-GROUP/providers/Microsoft.Web/sites/DEV-ATLAS-MATCH-PREDICTION-FUNCTION"
  key_set = "masterKey"
  key = "masterKey"
}

resource "azurerm_eventgrid_event_subscription" "donor-file-upload" {
  name                 = "${lower(var.ENVIRONMENT)}-donor-file-upload-subscription"
  scope                = data.terraform_remote_state.atlas.outputs.storage_account.id
  included_event_types = ["Microsoft.Storage.BlobCreated"]

  subject_filter {
    subject_begins_with = "/blobServices/default/containers/${data.terraform_remote_state.atlas.outputs.donor_import.storage.donor_container_name}"
  }

  webhook_endpoint {
    url = "https://${data.terraform_remote_state.atlas.outputs.donor_import.function_app.hostname}/runtime/webhooks/EventGrid?functionName=${local.import_donor_file_function_name}&code=${module.donor_import_function_key.function_key}"
  }
}

resource "azurerm_eventgrid_event_subscription" "haplotype-frequency-set-upload" {
  name                 = "${lower(var.ENVIRONMENT)}-haplotype-frequency-set-upload-subscription"
  scope                = data.terraform_remote_state.atlas.outputs.storage_account.id
  included_event_types = ["Microsoft.Storage.BlobCreated"]

  subject_filter {
    subject_begins_with = "/blobServices/default/containers/${data.terraform_remote_state.atlas.outputs.match_prediction.storage.haplotype_frequency_set_container_name}"
  }

  webhook_endpoint {
    url = "https://${data.terraform_remote_state.atlas.outputs.match_prediction.function_app.hostname}/runtime/webhooks/EventGrid?functionName=${local.import_haplotype_frequency_set_function_name}&code=${module.match_prediction_function_key.function_key}"
  }
}