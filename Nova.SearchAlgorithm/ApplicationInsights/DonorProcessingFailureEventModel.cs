using Nova.Utils.ApplicationInsights;
using Nova.Utils.ApplicationInsights.EventModels;
using System;

namespace Nova.SearchAlgorithm.ApplicationInsights
{
    public abstract class DonorProcessingFailureEventModel : EventModel
    {
        protected DonorProcessingFailureEventModel(
            string messageName,
            Exception exception, 
            string donorId) : base(messageName)
        {
            Level = LogLevel.Error;
            Properties.Add("Exception", exception.ToString());
            Properties.Add("DonorId", donorId);
        }
    }
}