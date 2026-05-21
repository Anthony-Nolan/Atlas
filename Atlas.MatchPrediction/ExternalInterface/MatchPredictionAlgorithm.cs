using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.MatchPrediction;
using Atlas.Common.ApplicationInsights;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.Common.Utils.Concurrency;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.ExternalInterface.ResultsUpload;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.MatchProbability;
using LoggingStopwatch;
using Atlas.Common.Utils.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.MatchPrediction.ExternalInterface
{
    public interface IMatchPredictionAlgorithm
    {
        public Task<MatchProbabilityResponse> RunMatchPredictionAlgorithm(SingleDonorMatchProbabilityInput singleDonorMatchProbabilityInput);

        /// <returns>A dictionary of donorId:filenames in blob storage where the per-donor results can be located.</returns>
        public Task<IReadOnlyDictionary<int, string>> RunMatchPredictionAlgorithmBatch(MultipleDonorMatchProbabilityInput multipleDonorMatchProbabilityInput);

        /// <summary>
        /// Same as <see cref="RunMatchPredictionAlgorithmBatch"/> but processes donors in parallel,
        /// each in its own DI scope, up to <paramref name="maxDegreeOfParallelism"/> concurrent tasks.
        /// </summary>
        /// <returns>A dictionary of donorId:filenames in blob storage where the per-donor results can be located.</returns>
        public Task<IReadOnlyDictionary<int, string>> RunMatchPredictionAlgorithmBatchParallel(
            MultipleDonorMatchProbabilityInput multipleDonorMatchProbabilityInput,
            int maxDegreeOfParallelism);

        public Task<HaplotypeFrequencySetResponse> GetHaplotypeFrequencySet(HaplotypeFrequencySetInput haplotypeFrequencySetInput);
    }

    internal class MatchPredictionAlgorithm : IMatchPredictionAlgorithm
    {
        private readonly IMatchProbabilityService matchProbabilityService;
        private readonly IGenotypeSetService genotypeSetService;
        private readonly IHaplotypeFrequencyService haplotypeFrequencyService;
        private readonly ISearchDonorResultUploader resultUploader;
        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly IAtlasLogger logger;

        public MatchPredictionAlgorithm(
            IMatchProbabilityService matchProbabilityService,
            IGenotypeSetService genotypeSetService,
            // ReSharper disable once SuggestBaseTypeForParameterInConstructor
            IMatchPredictionLogger<MatchProbabilityLoggingContext> logger,
            IHaplotypeFrequencyService haplotypeFrequencyService,
            ISearchDonorResultUploader resultUploader,
            IServiceScopeFactory serviceScopeFactory)
        {
            this.matchProbabilityService = matchProbabilityService;
            this.genotypeSetService = genotypeSetService;
            this.logger = logger;
            this.haplotypeFrequencyService = haplotypeFrequencyService;
            this.resultUploader = resultUploader;
            this.serviceScopeFactory = serviceScopeFactory;
        }

        /// <inheritdoc />
        public async Task<MatchProbabilityResponse> RunMatchPredictionAlgorithm(SingleDonorMatchProbabilityInput singleDonorMatchProbabilityInput)
        {
            using (logger.RunTimed("Run Match Prediction Algorithm"))
            {
                var patientGenotypeSet = await genotypeSetService.GetPatientGenotypeSet(singleDonorMatchProbabilityInput);
                var result = await matchProbabilityService.CalculateMatchProbability(singleDonorMatchProbabilityInput, patientGenotypeSet);
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
                var matchProbabilityInputs = multipleDonorMatchProbabilityInput.SingleDonorMatchProbabilityInputs.ToList();
                if (matchProbabilityInputs.Count == 0)
                {
                    return fileNames;
                }

                var patientGenotypeSet = await genotypeSetService.GetPatientGenotypeSet(matchProbabilityInputs.First());

                foreach (var matchProbabilityInput in matchProbabilityInputs)
                {
                    using (logger.RunTimed("Run Match Prediction Algorithm per donor"))
                    {
                        var result = await matchProbabilityService.CalculateMatchProbability(matchProbabilityInput, patientGenotypeSet);
                        var matchProbabilityInputFileNames = await resultUploader.UploadSearchDonorResults(searchRequestId, matchProbabilityInput.Donor.DonorIds, result);
                        fileNames = fileNames.Merge(matchProbabilityInputFileNames);
                    }
                }

                return fileNames;
            }
        }

        /// <inheritdoc />
        public async Task<IReadOnlyDictionary<int, string>> RunMatchPredictionAlgorithmBatchParallel(
            MultipleDonorMatchProbabilityInput multipleDonorMatchProbabilityInput,
            int maxDegreeOfParallelism)
        {
            using (logger.RunLongOperationWithTimer("Run Match Prediction Algorithm Batch (Parallel)", new LongLoggingSettings()))
            {
                var searchRequestId = multipleDonorMatchProbabilityInput.SearchRequestId;
                var matchProbabilityInputs = multipleDonorMatchProbabilityInput.SingleDonorMatchProbabilityInputs.ToList();
                if (matchProbabilityInputs.Count == 0)
                {
                    return new Dictionary<int, string>();
                }

                var patientGenotypeSet = await genotypeSetService.GetPatientGenotypeSet(matchProbabilityInputs.First());

                var perDonorResults = await matchProbabilityInputs.WhenAll(
                    async input =>
                    {
                        await using var scope = serviceScopeFactory.CreateAsyncScope();
                        var scopedMatchProbabilityService = scope.ServiceProvider.GetRequiredService<IMatchProbabilityService>();
                        var scopedResultUploader = scope.ServiceProvider.GetRequiredService<ISearchDonorResultUploader>();
                        var scopedLogger = scope.ServiceProvider.GetRequiredService<IMatchPredictionLogger<MatchProbabilityLoggingContext>>();

                        using (scopedLogger.RunTimed("Run Match Prediction Algorithm per donor (parallel)"))
                        {
                            var result = await scopedMatchProbabilityService.CalculateMatchProbability(input, patientGenotypeSet);
                            return await scopedResultUploader.UploadSearchDonorResults(searchRequestId, input.Donor.DonorIds, result);
                        }
                    },
                    maxDegreeOfParallelism);

                return perDonorResults
                    .Aggregate(new Dictionary<int, string>(), (fileNames, matchProbabilityInputFileNames) => fileNames.Merge(matchProbabilityInputFileNames));
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
