using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.MatchPrediction;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.Utils.Concurrency;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.ExternalInterface.ResultsUpload;
using Atlas.MatchPrediction.Services.MatchProbability;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.MatchPrediction.ExternalInterface
{
    public interface IParallelMatchPredictionAlgorithm
    {
        /// <summary>
        /// Runs match prediction for every donor in the batch and stores the whole batch's results in a single blob.
        /// </summary>
        /// <returns>The blob filename holding the batch's donor → result map, or <c>null</c> when the batch has no donors.</returns>
        Task<string> RunBatch(
            MultipleDonorMatchProbabilityInput multipleDonorMatchProbabilityInput,
            int maxDegreeOfParallelism,
            int batchId);
    }

    internal class ParallelMatchPredictionAlgorithm : IParallelMatchPredictionAlgorithm
    {
        private readonly IGenotypeSetService genotypeSetService;
        private readonly ISearchDonorResultUploader resultUploader;
        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly IAtlasLogger logger;

        public ParallelMatchPredictionAlgorithm(
            IGenotypeSetService genotypeSetService,
            ISearchDonorResultUploader resultUploader,
            // ReSharper disable once SuggestBaseTypeForParameterInConstructor
            IMatchPredictionLogger<MatchProbabilityLoggingContext> logger,
            IServiceScopeFactory serviceScopeFactory)
        {
            this.genotypeSetService = genotypeSetService;
            this.resultUploader = resultUploader;
            this.logger = logger;
            this.serviceScopeFactory = serviceScopeFactory;
        }

        public async Task<string> RunBatch(
            MultipleDonorMatchProbabilityInput multipleDonorMatchProbabilityInput,
            int maxDegreeOfParallelism,
            int batchId)
        {
            using (logger.RunLongOperationWithTimer("Run Match Prediction Algorithm Batch (Parallel)", new LongLoggingSettings()))
            {
                var searchRequestId = multipleDonorMatchProbabilityInput.SearchRequestId;
                var matchProbabilityInputs = multipleDonorMatchProbabilityInput.SingleDonorMatchProbabilityInputs.ToList();
                if (matchProbabilityInputs.Count == 0)
                {
                    return null;
                }

                var patientGenotypeSet = await genotypeSetService.GetPatientGenotypeSet(matchProbabilityInputs.First());

                var perDonorResults = await matchProbabilityInputs.WhenAll(
                    async input =>
                    {
                        await using var scope = serviceScopeFactory.CreateAsyncScope();
                        var scopedMatchProbabilityService = scope.ServiceProvider.GetRequiredService<IMatchProbabilityService>();
                        var scopedLogger = scope.ServiceProvider.GetRequiredService<IMatchPredictionLogger<MatchProbabilityLoggingContext>>();

                        using (scopedLogger.RunTimed("Run Match Prediction Algorithm per donor (parallel)"))
                        {
                            var result = await scopedMatchProbabilityService.CalculateMatchProbability(input, patientGenotypeSet);
                            return input.Donor.DonorIds.Select(donorId => new KeyValuePair<int, MatchProbabilityResponse>(donorId, result));
                        }
                    },
                    maxDegreeOfParallelism);

                var resultsByDonorId = perDonorResults
                    .SelectMany(donorResults => donorResults)
                    .ToDictionary(kv => kv.Key, kv => kv.Value);

                return await resultUploader.UploadBatchResult(searchRequestId, batchId, resultsByDonorId);
            }
        }
    }
}
