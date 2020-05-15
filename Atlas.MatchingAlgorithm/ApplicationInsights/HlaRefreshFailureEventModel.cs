using System;
using Atlas.Utils.Core.ApplicationInsights;

namespace Atlas.MatchingAlgorithm.ApplicationInsights
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