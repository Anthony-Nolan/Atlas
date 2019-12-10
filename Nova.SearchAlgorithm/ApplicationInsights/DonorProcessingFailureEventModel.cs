using Newtonsoft.Json;
using Nova.SearchAlgorithm.Models;
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
            FailedDonorInfo failedDonorInfo) : base(messageName)
        {
            Level = LogLevel.Error;
            Properties.Add("Exception", exception.ToString());
            Properties.Add("DonorId", failedDonorInfo.DonorId);
            Properties.Add("RegistryCode", failedDonorInfo.RegistryCode);
            Properties.Add("DonorInfo", JsonConvert.SerializeObject(failedDonorInfo.DonorInfo));
        }
    }
}