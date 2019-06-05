using System;
using Nova.Utils.ApplicationInsights;
using Nova.Utils.ApplicationInsights.EventModels;

namespace Nova.SearchAlgorithm.ApplicationInsights
{
    public class HlaRefreshFailureEventModel : EventModel
    {
        private const string MessageName = "Error running hla refresh job";
        
        public HlaRefreshFailureEventModel(Exception exception) : base(MessageName)
        {
            Level = LogLevel.Critical;
            Properties.Add("Message", exception.Message);
            Properties.Add("StackTrace", exception.StackTrace);
        }
    }
}