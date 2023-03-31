using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.MatchPrediction;
using Atlas.Common.ApplicationInsights;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.ExternalInterface.ResultsUpload;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.MatchProbability;
using LoggingStopwatch;
using Atlas.Common.Utils.Extensions;

namespace Atlas.MatchPrediction.ExternalInterface
{
    public interface IMatchPredictionAlgorithm
    {
        public Task<MatchProbabilityResponse> RunMatchPredictionAlgorithm(SingleDonorMatchProbabilityInput singleDonorMatchProbabilityInput);

        /// <returns>A dictionary of donorId:filenames in blob storage where the per-donor results can be located.</returns>
        public Task<IReadOnlyDictionary<int, string>> RunMatchPredictionAlgorithmBatch(MultipleDonorMatchProbabilityInput multipleDonorMatchProbabilityInput);

        public Task<HaplotypeFrequencySetResponse> GetHaplotypeFrequencySet(HaplotypeFrequencySetInput haplotypeFrequencySetInput);
    }

    internal class MatchPredictionAlgorithm : IMatchPredictionAlgorithm
    {
        private readonly IMatchProbabilityService matchProbabilityService;
        private readonly IHaplotypeFrequencyService haplotypeFrequencyService;
        private readonly ISearchDonorResultUploader resultUploader;
        private readonly ILogger logger;

        public MatchPredictionAlgorithm(
            IMatchProbabilityService matchProbabilityService,
            // ReSharper disable once SuggestBaseTypeForParameterInConstructor
            IMatchPredictionLogger<MatchProbabilityLoggingContext> logger,
            IHaplotypeFrequencyService haplotypeFrequencyService,
            ISearchDonorResultUploader resultUploader)
        {
            this.matchProbabilityService = matchProbabilityService;
            this.logger = logger;
            this.haplotypeFrequencyService = haplotypeFrequencyService;
            this.resultUploader = resultUploader;
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
        public async Task<IReadOnlyDictionary<int, string>> RunMatchPredictionAlgorithmBatch(
            MultipleDonorMatchProbabilityInput multipleDonorMatchProbabilityInput)
        {
            using (logger.RunLongOperationWithTimer("Run Match Prediction Algorithm Batch", new LongLoggingSettings()))
            {
                var searchRequestId = multipleDonorMatchProbabilityInput.SearchRequestId;
                var fileNames = new Dictionary<int, string>();
                foreach (var matchProbabilityInput in multipleDonorMatchProbabilityInput.SingleDonorMatchProbabilityInputs)
                {
                    using (logger.RunTimed("Run Match Prediction Algorithm per donor"))
                    {
                        var result = await matchProbabilityService.CalculateMatchProbability(matchProbabilityInput);
                        var matchProbabilityInputFileNames = await resultUploader.UploadSearchDonorResults(searchRequestId, matchProbabilityInput.Donor.DonorIds, result);
                        fileNames = fileNames.Merge(matchProbabilityInputFileNames).ToDictionary();
                    }
                }

                return fileNames;
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