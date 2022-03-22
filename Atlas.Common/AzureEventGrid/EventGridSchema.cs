using System;

namespace Atlas.Common.AzureEventGrid
{
    /// <summary>
    /// Model containing properties needed in Atlas in the Event Grid schema as documented here: https://docs.microsoft.com/en-us/azure/event-grid/event-schema#event-properties
    ///
    /// The officially provided class version of this schema, EventGridEvent, is documented here: https://github.com/Azure/azure-sdk-for-net/blob/Azure.Messaging.EventGrid_4.0.0-beta.2/sdk/eventgrid/Azure.Messaging.EventGrid/README.md
    /// We cannot use this model ourselves, as it is not a valid function binding parameter - the class is not serialisable.
    /// We also cannot just accept a string and use the static `Parse` method to generate an EventGridEvent model, as we are using data binding in blob storage triggers, which requires the configured model to have a known schema.
    ///
    /// As such we are creating a minimal, serialisable, version of the EventGrid schema, which can be used in our use case.
    /// This must be updated:
    /// * to include any new properties Atlas may rely on from this schema
    /// * if any changes are made by Microsoft to the EventGrid schema 
    /// </summary>
    public class EventGridSchema
    {
        public string Subject { get; set; }
        public DateTimeOffset EventTime { get; set; }
        public EventGridData Data { get; set; }
    }

    public class EventGridData
    {
        public string Url { get; set; }
    }
}