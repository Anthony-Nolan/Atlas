using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
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
        private readonly ILogger logger;

        public MatchPredictionAlgorithm(IMatchProbabilityService matchProbabilityService, ILogger logger)
        {
            this.matchProbabilityService = matchProbabilityService;
            this.logger = logger;
        }

        /// <inheritdoc />
        public async Task<MatchProbabilityResponse> RunMatchPredictionAlgorithm(MatchProbabilityInput matchProbabilityInput)
        {
            return await logger.RunTimedAsync(
                async () => await matchProbabilityService.CalculateMatchProbability(matchProbabilityInput),
                "Match Prediction Algorithm Completed"
            );
        }
    }
}