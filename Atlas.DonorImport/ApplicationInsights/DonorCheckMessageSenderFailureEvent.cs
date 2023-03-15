using System;
using Atlas.Common.ApplicationInsights;

namespace Atlas.DonorImport.ApplicationInsights
{
    internal class DonorCheckMessageSenderFailureEvent : EventModel
    {
        public DonorCheckMessageSenderFailureEvent(Exception exception, string message) : base("Error Sending Donors Check Message")
        {
            Level = LogLevel.Warn;
            Properties.Add("Exception", exception.ToString());
            Properties.Add("Message", message);
        }
    }
}
