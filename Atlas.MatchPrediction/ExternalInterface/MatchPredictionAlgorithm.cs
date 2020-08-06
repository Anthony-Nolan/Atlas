using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.MatchProbability;

namespace Atlas.MatchPrediction.ExternalInterface
{
    public interface IMatchPredictionAlgorithm
    {
        public Task<MatchProbabilityResponse> RunMatchPredictionAlgorithm(SingleDonorMatchProbabilityInput singleDonorMatchProbabilityInput);

        public Task<HaplotypeFrequencySetResponse> GetHaplotypeFrequencySet(HaplotypeFrequencySetInput haplotypeFrequencySetInput);
    }

    internal class MatchPredictionAlgorithm : IMatchPredictionAlgorithm
    {
        private readonly IMatchProbabilityService matchProbabilityService;
        private readonly IHaplotypeFrequencyService haplotypeFrequencyService;
        private readonly ILogger logger;

        public MatchPredictionAlgorithm(
            IMatchProbabilityService matchProbabilityService,
            // ReSharper disable once SuggestBaseTypeForParameter
            IMatchPredictionLogger logger,
            IHaplotypeFrequencyService haplotypeFrequencyService)
        {
            this.matchProbabilityService = matchProbabilityService;
            this.logger = logger;
            this.haplotypeFrequencyService = haplotypeFrequencyService;
        }

        /// <inheritdoc />
        public async Task<MatchProbabilityResponse> RunMatchPredictionAlgorithm(SingleDonorMatchProbabilityInput singleDonorMatchProbabilityInput)
        {
            using (logger.RunTimed("Run Match Prediction Algorithm"))
            {
                var result = await matchProbabilityService.CalculateMatchProbability(singleDonorMatchProbabilityInput);
                return result.Round(4);
            }
        }

        public async Task<HaplotypeFrequencySetResponse> GetHaplotypeFrequencySet(HaplotypeFrequencySetInput haplotypeFrequencySetInput)
        {
            return await haplotypeFrequencyService.GetHaplotypeFrequencySets(
                haplotypeFrequencySetInput.DonorInfo,
                haplotypeFrequencySetInput.PatientInfo);
        }
    }
}