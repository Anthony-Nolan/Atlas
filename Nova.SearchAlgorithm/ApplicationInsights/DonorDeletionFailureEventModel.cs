using System;
using Nova.Utils.ApplicationInsights;
using Nova.Utils.ApplicationInsights.EventModels;

namespace Nova.SearchAlgorithm.ApplicationInsights
{
    public class DonorDeletionFailureEventModel : EventModel
    {
        private const string MessageName = "Error when attempting to delete the donor.";

        public DonorDeletionFailureEventModel(Exception exception, string donorId) : base(MessageName)
        {
            Level = LogLevel.Error;
            Properties.Add("Exception", exception.ToString());
            Properties.Add("DonorId", donorId);
        }
    }
}