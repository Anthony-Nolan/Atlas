using Atlas.Common.ApplicationInsights;
using System;

namespace Atlas.DonorImport.ApplicationInsights
{
    internal class DonorImportMessageSenderFailureEvent : EventModel
    {
        public DonorImportMessageSenderFailureEvent(Exception exception, string message) : base("Error Sending Donor Import Message")
        {
            Level = LogLevel.Warn;
            Properties.Add("Exception", exception.ToString());
            Properties.Add("Message", message);
        }
    }
}
