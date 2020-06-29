using System.Threading.Tasks;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Services.MatchProbability;

namespace Atlas.MatchPrediction.ExternalInterface
{
    public interface IMatchPredictionAlgorithm
    {
        public Task<MatchProbabilityResponse> RunMatchPredictionAlgorithm(MatchProbabilityInput matchProbabilityInput);
    }

    internal class MatchPredictionAlgorithm : IMatchPredictionAlgorithm
    {
        private readonly IMatchProbabilityService matchProbabilityService;

        public MatchPredictionAlgorithm(IMatchProbabilityService matchProbabilityService)
        {
            this.matchProbabilityService = matchProbabilityService;
        }
        
        /// <inheritdoc />
        public async Task<MatchProbabilityResponse> RunMatchPredictionAlgorithm(MatchProbabilityInput matchProbabilityInput)
        {
            return await matchProbabilityService.CalculateMatchProbability(matchProbabilityInput);
        }
    }
}