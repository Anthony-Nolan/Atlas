resource "azurerm_eventgrid_event_subscription" "donor-file-upload-to-service-bus" {
  name  = "${lower(var.general.environment)}-donor-file-upload-to-service-bus"
  scope = var.azure_storage.id
  included_event_types = [
    "Microsoft.Storage.BlobCreated"
  ]

  subject_filter {
    subject_begins_with = "/blobServices/default/containers/${azurerm_storage_container.donor_blob_storage.name}"
  }

  service_bus_topic_endpoint_id = azurerm_servicebus_topic.donor-import-file-uploads.id
}