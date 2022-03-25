using System;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace Atlas.Functions.Models
{
    public class SearchRequestTrackingInfo
    {
        public string SearchRequestId { get; set; }
        public int? NumberOfResults { get; set; }
        public string OrchestrationStatus { get; set; }
        public int MatchPredictionCompletedDonorCount { get; set; }
        public DateTime OrchestrationStarted { get; set; }
        public OrchestrationRuntimeStatus RuntimeStatus { get; set; }
    }
}