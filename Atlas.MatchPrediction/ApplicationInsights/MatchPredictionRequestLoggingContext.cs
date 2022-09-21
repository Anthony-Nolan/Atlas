using System.Collections.Generic;
using Atlas.MatchPrediction.ExternalInterface.Models;

namespace Atlas.MatchPrediction.ApplicationInsights
{
    public class MatchPredictionRequestLoggingContext : MatchProbabilityLoggingContext
    {
        public string MatchPredictionRequestId { get; set; }

        public void Initialise(IdentifiedMatchPredictionRequest request)
        {
            MatchPredictionRequestId = request.Id;
            Initialise(request.SingleDonorMatchProbabilityInput);
        }

        public override Dictionary<string, string> PropertiesToLog()
        {
            var props = new Dictionary<string, string>(base.PropertiesToLog())
            {
                {nameof(MatchPredictionRequestId), MatchPredictionRequestId}
            };

            return props;
        }
    }
}