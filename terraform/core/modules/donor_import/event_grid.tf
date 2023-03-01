resource "azurerm_eventgrid_event_subscription" "donor-file-upload-to-service-bus" {
  name  = "${lower(var.general.environment)}-donor-file-upload-to-service-bus"
  scope = var.azure_storage.id
  included_event_types = [
    "Microsoft.Storage.BlobCreated"
  ]
  
  advanced_filter {
	string_begins_with {
		key = "Subject"
		values = ["/blobServices/default/containers/${azurerm_storage_container.donor_blob_storage.name}"]
	}
	string_not_begins_with {
		key = "Subject"
		values = ["/blobServices/default/containers/${azurerm_storage_container.donor_blob_storage.name}/donor-id-checker"]
	}
  }  

  service_bus_topic_endpoint_id = azurerm_servicebus_topic.donor-import-file-uploads.id
}

resource "azurerm_eventgrid_event_subscription" "donor-id-checker-request-to-service-bus" {
  name  = "${lower(var.general.environment)}-donor-id-checker-request-to-service-bus"
  scope = var.azure_storage.id
  included_event_types = [
    "Microsoft.Storage.BlobCreated"
  ]

  subject_filter {
    subject_begins_with = "/blobServices/default/containers/${azurerm_storage_container.donor_blob_storage.name}/donor-id-checker/requests"
  }

  service_bus_topic_endpoint_id = azurerm_servicebus_topic.donor-id-checker-requests.id
}