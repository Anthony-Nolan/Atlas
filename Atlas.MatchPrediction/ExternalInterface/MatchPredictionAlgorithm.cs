using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Services.HaplotypeFrequencySets;
using Atlas.MatchPrediction.Services.MatchProbability;
using HaplotypeFrequencySet = Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet.HaplotypeFrequencySet;

namespace Atlas.MatchPrediction.ExternalInterface
{
    public interface IMatchPredictionAlgorithm
    {
        public Task<MatchProbabilityResponse> RunMatchPredictionAlgorithm(MatchProbabilityInput matchProbabilityInput);

        public Task<HaplotypeFrequencySetResponse> GetHaplotypeFrequencySet(
            HaplotypeFrequencySetInput haplotypeFrequencySetInput);
    }

    internal class MatchPredictionAlgorithm : IMatchPredictionAlgorithm
    {
        private readonly IMatchProbabilityService matchProbabilityService;
        private readonly IHaplotypeFrequencySetService haplotypeFrequencySetService;
        private readonly ILogger logger;

        public MatchPredictionAlgorithm(IMatchProbabilityService matchProbabilityService, ILogger logger, IHaplotypeFrequencySetService haplotypeFrequencySetService)
        {
            this.matchProbabilityService = matchProbabilityService;
            this.logger = logger;
            this.haplotypeFrequencySetService = haplotypeFrequencySetService;
        }

        /// <inheritdoc />
        public async Task<MatchProbabilityResponse> RunMatchPredictionAlgorithm(MatchProbabilityInput matchProbabilityInput)
        {
            return await logger.RunTimedAsync(
                async () => await matchProbabilityService.CalculateMatchProbability(matchProbabilityInput),
                "Match Prediction Algorithm Completed"
            );
        }

        public async Task<HaplotypeFrequencySetResponse> GetHaplotypeFrequencySet(HaplotypeFrequencySetInput haplotypeFrequencySetInput)
        {
            return await haplotypeFrequencySetService.GetHaplotypeFrequencySets(haplotypeFrequencySetInput.DonorInfo,
                haplotypeFrequencySetInput.PatientInfo);
        }
    }
}