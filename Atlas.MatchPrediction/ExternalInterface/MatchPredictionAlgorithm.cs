using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.MatchPrediction;
using Atlas.Common.ApplicationInsights;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.ExternalInterface.ResultsUpload;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.MatchProbability;
using Atlas.MatchPrediction.Services.Precompute;
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
        private readonly IGenotypeSetService genotypeSetService;
        private readonly IHaplotypeFrequencyService haplotypeFrequencyService;
        private readonly ISearchDonorResultUploader resultUploader;
        private readonly IDonorGenotypeSetSourceResolver genotypeSetSourceResolver;
        private readonly IDonorGenotypeSetBatchCompleter genotypeSetBatchCompleter;
        private readonly IPatientGenotypeSetProvider patientGenotypeSetProvider;
        private readonly IAtlasLogger logger;

        public MatchPredictionAlgorithm(
            IMatchProbabilityService matchProbabilityService,
            IGenotypeSetService genotypeSetService,
            // ReSharper disable once SuggestBaseTypeForParameterInConstructor
            IMatchPredictionLogger<MatchProbabilityLoggingContext> logger,
            IHaplotypeFrequencyService haplotypeFrequencyService,
            ISearchDonorResultUploader resultUploader,
            IDonorGenotypeSetSourceResolver genotypeSetSourceResolver,
            IDonorGenotypeSetBatchCompleter genotypeSetBatchCompleter,
            IPatientGenotypeSetProvider patientGenotypeSetProvider)
        {
            this.matchProbabilityService = matchProbabilityService;
            this.genotypeSetService = genotypeSetService;
            this.logger = logger;
            this.haplotypeFrequencyService = haplotypeFrequencyService;
            this.resultUploader = resultUploader;
            this.genotypeSetSourceResolver = genotypeSetSourceResolver;
            this.genotypeSetBatchCompleter = genotypeSetBatchCompleter;
            this.patientGenotypeSetProvider = patientGenotypeSetProvider;
        }

        /// <inheritdoc />
        public async Task<MatchProbabilityResponse> RunMatchPredictionAlgorithm(SingleDonorMatchProbabilityInput singleDonorMatchProbabilityInput)
        {
            using (logger.RunTimed("Run Match Prediction Algorithm"))
            {
                var patientGenotypeSet = await genotypeSetService.GetPatientGenotypeSet(singleDonorMatchProbabilityInput);

                // Always live, with no precompute lookup and no usage event. This is the standalone match prediction
                // path (one call per donor, outside search), which has no record of the matching database and so could
                // never use a stored set; an event per donor would only add volume.
                var result = await matchProbabilityService.CalculateMatchProbability(singleDonorMatchProbabilityInput, patientGenotypeSet);

                return result.Response.Round(4);
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

                // Once per batch, not per donor: one read of the stored rows, and one decision about the kill-switch.
                var batchContext = await genotypeSetSourceResolver.Resolve(
                    multipleDonorMatchProbabilityInput,
                    multipleDonorMatchProbabilityInput.Donors);

                var (patientGenotypeSet, patientGenotypeSetSource) =
                    await patientGenotypeSetProvider.Get(matchProbabilityInputs.First(), batchContext);
                var outcomes = new List<DonorGenotypeSetBatchOutcome>(matchProbabilityInputs.Count);

                foreach (var matchProbabilityInput in matchProbabilityInputs)
                {
                    using (logger.RunTimed("Run Match Prediction Algorithm per donor"))
                    {
                        var result = await matchProbabilityService.CalculateMatchProbability(matchProbabilityInput, patientGenotypeSet, batchContext);
                        var matchProbabilityInputFileNames = await resultUploader.UploadSearchDonorResults(searchRequestId, matchProbabilityInput.Donor.DonorIds, result.Response);
                        fileNames = fileNames.Merge(matchProbabilityInputFileNames);
                        outcomes.Add(ToOutcome(matchProbabilityInput, result));
                    }
                }

                // After every donor's results are uploaded. This activity returns only after the store, so the store
                // does delay the end of the batch, and so of the search. The store's lock timeout keeps that delay short.
                await genotypeSetBatchCompleter.Complete(multipleDonorMatchProbabilityInput, batchContext, outcomes, patientGenotypeSetSource);

                return fileNames;
            }
        }

        private static DonorGenotypeSetBatchOutcome ToOutcome(SingleDonorMatchProbabilityInput input, MatchProbabilityResult result) =>
            new(input.Donor.DonorIds.Count, result.GenotypeSetSource, result.GenotypeSetToStore);

        public async Task<HaplotypeFrequencySetResponse> GetHaplotypeFrequencySet(HaplotypeFrequencySetInput haplotypeFrequencySetInput)
        {
            return await haplotypeFrequencyService.GetHaplotypeFrequencySets(
                haplotypeFrequencySetInput.DonorInfo,
                haplotypeFrequencySetInput.PatientInfo);
        }

    }
}
