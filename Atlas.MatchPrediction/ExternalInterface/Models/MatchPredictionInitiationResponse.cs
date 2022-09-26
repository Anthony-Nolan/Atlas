using System.Collections.Generic;
using FluentValidation.Results;

namespace Atlas.MatchPrediction.ExternalInterface.Models
{
    public class MatchPredictionInitiationResponse
    {
        public string MatchPredictionRequestId { get; set; }
    }

    public class BatchedMatchPredictionInitiationResponse
    {
        public List<DonorResponse> DonorResponses { get; set; }
    }

    public class DonorResponse
    {
        public int? DonorId { get; set; }

        /// <summary>
        /// Only populated if donor info supplied in original request was valid
        /// </summary>
        public string MatchPredictionRequestId { get; set; }

        /// <summary>
        /// Only populated if donor info was invalid
        /// </summary>
        public List<ValidationFailure> ValidationErrors { get; set; }
    }
}