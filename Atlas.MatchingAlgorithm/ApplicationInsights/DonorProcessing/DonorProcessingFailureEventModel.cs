using System;
using Newtonsoft.Json;
using Atlas.MatchingAlgorithm.Models;
using Nova.Utils.ApplicationInsights;
using Nova.Utils.ApplicationInsights.EventModels;

namespace Atlas.MatchingAlgorithm.ApplicationInsights.DonorProcessing
{
    public abstract class DonorProcessingFailureEventModel : EventModel
    {
        protected DonorProcessingFailureEventModel(
            string eventName,
            Exception exception,
            FailedDonorInfo failedDonorInfo) : base(eventName)
        {
            Level = LogLevel.Error;
            Properties.Add("Exception", exception.ToString());
            Properties.Add("DonorId", failedDonorInfo.DonorId);
            Properties.Add("DonorInfo", JsonConvert.SerializeObject(failedDonorInfo.DonorInfo));
        }
    }
}