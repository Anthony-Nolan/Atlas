using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.ExternalInterface.ResultsUpload;
using Atlas.MatchPrediction.Services.MatchProbability;
using LoggingStopwatch;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.MatchPrediction.ExternalInterface
{
    public interface IParallelMatchPredictionAlgorithm
    {
        /// <returns>A dictionary of donorId:filenames in blob storage where the per-donor results can be located.</returns>
        Task<IReadOnlyDictionary<int, string>> RunBatch(
            MultipleDonorMatchProbabilityInput multipleDonorMatchProbabilityInput);
    }

    internal class ParallelMatchPredictionAlgorithm : IParallelMatchPredictionAlgorithm
    {
        private readonly IGenotypeSetService genotypeSetService;
        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly IAtlasLogger logger;
        private readonly IDonorProcessingLimiter donorProcessingLimiter;

        public ParallelMatchPredictionAlgorithm(
            IGenotypeSetService genotypeSetService,
            // ReSharper disable once SuggestBaseTypeForParameterInConstructor
            IMatchPredictionLogger<MatchProbabilityLoggingContext> logger,
            IServiceScopeFactory serviceScopeFactory,
            IDonorProcessingLimiter donorProcessingLimiter)
        {
            this.genotypeSetService = genotypeSetService;
            this.logger = logger;
            this.serviceScopeFactory = serviceScopeFactory;
            this.donorProcessingLimiter = donorProcessingLimiter;
        }

        public async Task<IReadOnlyDictionary<int, string>> RunBatch(
            MultipleDonorMatchProbabilityInput multipleDonorMatchProbabilityInput)
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

                // All donors across all concurrently-processed batch messages share one process-wide concurrency
                // budget (see IDonorProcessingLimiter), so a slot freed by a draining batch is immediately reused by
                // another batch — keeping the budget saturated without raising peak donor concurrency (and thus memory).
                var perDonorResults = await Task.WhenAll(matchProbabilityInputs.Select(input =>
                    donorProcessingLimiter.RunAsync(() => ProcessDonorAsync(input, searchRequestId, patientGenotypeSet))));

                return perDonorResults
                    .Aggregate(new Dictionary<int, string>(), (fileNames, d) => fileNames.Merge(d));
            }
        }

        private async Task<Dictionary<int, string>> ProcessDonorAsync(SingleDonorMatchProbabilityInput input, string searchRequestId, MatchPrediction.Models.SubjectGenotypeSet patientGenotypeSet)
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
        }
    }
}
