using System;
using Atlas.Client.Models.Search.Results.Matching;

namespace Atlas.Functions.Models
{
    public class SearchOrchestratorParameters
    {
        public MatchingResultsNotification MatchingResultsNotification { get; set; }
        public DateTimeOffset InitiationTime { get; set; }
    }
}
