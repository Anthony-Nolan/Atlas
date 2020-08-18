using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.MatchPrediction;
using Atlas.Common.ApplicationInsights;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.MatchProbability;
using Atlas.MatchPrediction.Validators;
using FluentValidation.Results;
using LoggingStopwatch;

namespace Atlas.MatchPrediction.ExternalInterface
{
    public interface IMatchPredictionAlgorithm
    {
        public Task<MatchProbabilityResponse> RunMatchPredictionAlgorithm(SingleDonorMatchProbabilityInput singleDonorMatchProbabilityInput);

        public ValidationResult ValidateMatchPredictionAlgorithmInput(SingleDonorMatchProbabilityInput singleDonorMatchProbabilityInput);

        /// <returns>A dictionary of DonorIds to Match Prediction Result</returns>
        public Task<IReadOnlyDictionary<int, MatchProbabilityResponse>> RunMatchPredictionAlgorithmBatch(
            MultipleDonorMatchProbabilityInput multipleDonorMatchProbabilityInput);

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

        /// <inheritdoc />
        public async Task<IReadOnlyDictionary<int, MatchProbabilityResponse>> RunMatchPredictionAlgorithmBatch(
            MultipleDonorMatchProbabilityInput multipleDonorMatchProbabilityInput)
        {
            using (logger.RunLongOperationWithTimer("Run Match Prediction Algorithm Batch", new LongLoggingSettings()))
            {
                var results = new Dictionary<int, MatchProbabilityResponse>();
                foreach (var matchProbabilityInput in multipleDonorMatchProbabilityInput.SingleDonorMatchProbabilityInputs)
                {
                    using (logger.RunTimed("Run Match Prediction Algorithm per donor"))
                    {
                        var result = await matchProbabilityService.CalculateMatchProbability(matchProbabilityInput);
                        foreach (var donorId in matchProbabilityInput.DonorInput.DonorIds)
                        {
                            results[donorId] = result;
                        }
                    }
                }

                return results;
            }
        }

        public async Task<HaplotypeFrequencySetResponse> GetHaplotypeFrequencySet(HaplotypeFrequencySetInput haplotypeFrequencySetInput)
        {
            return await haplotypeFrequencyService.GetHaplotypeFrequencySets(
                haplotypeFrequencySetInput.DonorInfo,
                haplotypeFrequencySetInput.PatientInfo);
        }

        public ValidationResult ValidateMatchPredictionAlgorithmInput(SingleDonorMatchProbabilityInput singleDonorMatchProbabilityInput)
        {
            return new MatchProbabilityNonDonorValidator().Validate(singleDonorMatchProbabilityInput);
        }
    }
}