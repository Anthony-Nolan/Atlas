resource "azurerm_eventgrid_event_subscription" "haplotype-frequency-file-upload-to-service-bus" {
  name  = "${lower(var.general.environment)}-haplotype-frequency-file-upload-to-service-bus"
  scope = var.azure_storage.id
  included_event_types = [
    "Microsoft.Storage.BlobCreated"
  ]

  subject_filter {
    subject_begins_with = "/blobServices/default/containers/${azurerm_storage_container.haplotype_frequency_set_blob_container.name}"
  }

  service_bus_topic_endpoint_id = azurerm_servicebus_topic.haplotype-frequency-file-uploads.id
}