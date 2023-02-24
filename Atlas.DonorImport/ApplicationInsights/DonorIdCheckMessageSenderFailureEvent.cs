using System;
using Atlas.Common.ApplicationInsights;

namespace Atlas.DonorImport.ApplicationInsights
{
    internal class DonorIdCheckMessageSenderFailureEvent : EventModel
    {
        public DonorIdCheckMessageSenderFailureEvent(Exception exception, string message) : base("Error Sending Donor Ids Check Message")
        {
            Level = LogLevel.Warn;
            Properties.Add("Exception", exception.ToString());
            Properties.Add("Message", message);
        }
    }
}
