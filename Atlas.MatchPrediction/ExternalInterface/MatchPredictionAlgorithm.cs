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
using Atlas.MatchPrediction.Services.ResultsUpload;
using LoggingStopwatch;

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
        private readonly IResultUploader resultUploader;
        private readonly ILogger logger;

        public MatchPredictionAlgorithm(
            IMatchProbabilityService matchProbabilityService,
            // ReSharper disable once SuggestBaseTypeForParameter
            IMatchPredictionLogger logger,
            IHaplotypeFrequencyService haplotypeFrequencyService,
            IResultUploader resultUploader)
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
                        foreach (var donorId in matchProbabilityInput.Donor.DonorIds)
                        {
                            var fileName = await resultUploader.UploadDonorResult(searchRequestId, donorId, result);
                            fileNames[donorId] = fileName;
                        }
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