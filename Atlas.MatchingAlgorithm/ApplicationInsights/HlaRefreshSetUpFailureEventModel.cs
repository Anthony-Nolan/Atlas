using System;
using Atlas.Common.ApplicationInsights;

namespace Atlas.MatchingAlgorithm.ApplicationInsights
{
    public class HlaRefreshSetUpFailureEventModel : EventModel
    {
        private const string MessageName = "Error running upfront setup for hla refresh job";
        
        public HlaRefreshSetUpFailureEventModel(Exception exception) : base(MessageName)
        {
            Level = LogLevel.Critical;
            Properties.Add("Message", exception.Message);
            Properties.Add("StackTrace", exception.StackTrace);
        }
    }
}