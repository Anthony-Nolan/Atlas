using System;

namespace Atlas.Utils.Core.ApplicationInsights.EventModels
{
    public class ReadPrivateKeyErrorEventModel : EventModel
    {
        private const string MessageName = "Error retrieving/reading private key from key vault";

        public ReadPrivateKeyErrorEventModel(Exception e) : base(MessageName)
        {
            Level = LogLevel.Error;
            Properties.Add("Exception", e.ToString());
        }
    }
}