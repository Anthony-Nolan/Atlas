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
    /// <summary>
    /// Outcome of running a parallel match-prediction batch: the result blob location plus the post-truncation imputed
    /// genotype counts captured en route (see ATL-252), bounded by
    /// <see cref="Atlas.MatchPrediction.ExternalInterface.Settings.GenotypeImputationSettings.MaximumExpandedGenotypesPerInput"/>.
    /// </summary>
    /// <param name="ResultLocation">Blob filename holding the batch's donor → result map, or <c>null</c> when the batch had no donors.</param>
    /// <param name="PatientGenotypeCount">Patient imputed genotype count (same across a run's batches; 0 when the batch had no donors).</param>
    /// <param name="DonorGenotypeCounts">Per-donor imputed genotype count, keyed by donor id, covering every donor in the batch.</param>
    public record ParallelMatchPredictionBatchOutput(
        string ResultLocation,
        int PatientGenotypeCount,
        IReadOnlyDictionary<int, int> DonorGenotypeCounts);

    public interface IParallelMatchPredictionAlgorithm
    {
        /// <summary>
        /// Runs match prediction for every donor in the batch and stores the whole batch's results in a single blob.
        /// </summary>
        /// <returns>
        /// The result blob location together with the imputed genotype counts for the patient and each donor in the
        /// batch (see <see cref="ParallelMatchPredictionBatchOutput"/>).
        /// </returns>
        Task<ParallelMatchPredictionBatchOutput> RunBatch(
            MultipleDonorMatchProbabilityInput multipleDonorMatchProbabilityInput,
            int maxDegreeOfParallelism,
            int batchId);
    }

    internal class ParallelMatchPredictionAlgorithm : IParallelMatchPredictionAlgorithm
    {
        private readonly IGenotypeSetService genotypeSetService;
        private readonly IMatchPredictionBatchResultUploader resultUploader;
        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly IAtlasLogger logger;

        public ParallelMatchPredictionAlgorithm(
            IGenotypeSetService genotypeSetService,
            IMatchPredictionBatchResultUploader resultUploader,
            // ReSharper disable once SuggestBaseTypeForParameterInConstructor
            IMatchPredictionLogger<MatchProbabilityLoggingContext> logger,
            IServiceScopeFactory serviceScopeFactory)
        {
            this.genotypeSetService = genotypeSetService;
            this.resultUploader = resultUploader;
            this.logger = logger;
            this.serviceScopeFactory = serviceScopeFactory;
        }

        public async Task<ParallelMatchPredictionBatchOutput> RunBatch(
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
                    return new ParallelMatchPredictionBatchOutput(null, 0, new Dictionary<int, int>());
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
                            // Donors sharing a phenotype are imputed once, so every id in the group carries the same result and count.
                            return input.Donor.DonorIds.Select(donorId =>
                                new DonorBatchResult(donorId, result.Response, result.DonorGenotypeCount));
                        }
                    },
                    maxDegreeOfParallelism);

                var flattenedResults = perDonorResults.SelectMany(donorResults => donorResults).ToList();

                var resultsByDonorId = flattenedResults.ToDictionary(r => r.DonorId, r => r.Response);
                var donorGenotypeCounts = flattenedResults.ToDictionary(r => r.DonorId, r => r.GenotypeCount);

                var resultLocation = await resultUploader.UploadMatchPredictionBatchResult(searchRequestId, batchId, resultsByDonorId);

                return new ParallelMatchPredictionBatchOutput(resultLocation, patientGenotypeSet.Genotypes.Count, donorGenotypeCounts);
            }
        }

        private sealed record DonorBatchResult(int DonorId, MatchProbabilityResponse Response, int GenotypeCount);
    }
}
