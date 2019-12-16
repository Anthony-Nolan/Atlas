using System;
using Newtonsoft.Json;
using Nova.SearchAlgorithm.Models;
using Nova.Utils.ApplicationInsights;
using Nova.Utils.ApplicationInsights.EventModels;

namespace Nova.SearchAlgorithm.ApplicationInsights.DonorProcessing
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
            Properties.Add("RegistryCode", failedDonorInfo.RegistryCode);
            Properties.Add("DonorInfo", JsonConvert.SerializeObject(failedDonorInfo.DonorInfo));
        }
    }
}