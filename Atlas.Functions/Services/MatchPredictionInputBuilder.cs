using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Requests;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.ResultSet;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.Functions.Settings;
using Atlas.MatchPrediction.ExternalInterface;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Services.Precompute;
using EnumStringValues;
using Microsoft.Extensions.Options;

namespace Atlas.Functions.Services
{
    public interface IMatchPredictionInputBuilder
    {
        /// <summary>
        /// Builds the search's match prediction batch inputs. Also stores the patient's genotype set, once per search,
        /// when the search uses stored sets, and puts its key on every batch (<see cref="IPatientGenotypeSetWarmer"/>).
        /// </summary>
        Task<IEnumerable<MultipleDonorMatchProbabilityInput>> BuildMatchPredictionInputs(
            ResultSet<MatchingAlgorithmResult> matchingResultSet,
            int? batchSizeOverride = null);
    }

    internal class MatchPredictionInputBuilder : IMatchPredictionInputBuilder
    {
        private readonly IAtlasLogger logger;
        private readonly IDonorInputBatcher donorInputBatcher;
        private readonly IPatientGenotypeSetWarmer patientGenotypeSetWarmer;
        private readonly int matchPredictionBatchSize;

        public MatchPredictionInputBuilder(
            ISearchLogger<SearchLoggingContext> logger,
            IDonorInputBatcher donorInputBatcher,
            IPatientGenotypeSetWarmer patientGenotypeSetWarmer,
            IOptions<OrchestrationSettings> orchestrationSettings)
        {
            this.logger = logger;
            this.donorInputBatcher = donorInputBatcher;
            this.patientGenotypeSetWarmer = patientGenotypeSetWarmer;
            matchPredictionBatchSize = orchestrationSettings.Value.MatchPredictionBatchSize;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<MultipleDonorMatchProbabilityInput>> BuildMatchPredictionInputs(
            ResultSet<MatchingAlgorithmResult> matchingResultSet,
            int? batchSizeOverride = null)
        {
            using (logger.RunTimed($"Building match prediction inputs: {matchingResultSet.SearchRequestId}"))
            {
                var nonDonorInput = BuildSearchRequestMatchPredictionInput(matchingResultSet);
                var donorInputs = matchingResultSet.Results.Select(BuildPerDonorMatchPredictionInput).ToList();

                // Once per search, before the batch inputs are copied from it, so every batch carries the key. With no
                // donors there are no batches to use the set, so the warm step only logs that it was skipped.
                nonDonorInput.PatientGenotypeSetKey = await patientGenotypeSetWarmer.Warm(nonDonorInput, hasDonors: donorInputs.Count > 0);

                return donorInputBatcher.BatchDonorInputs(nonDonorInput, donorInputs, batchSizeOverride > 0 ? batchSizeOverride.Value : matchPredictionBatchSize).ToList();
            }
        }

        /// <summary>
        /// Builds all non-donor information required to run the match prediction algorithm for a search request.
        /// e.g. patient info, matching preferences
        /// 
        /// This will remain constant for all donors in the request, so only needs to be calculated once.
        /// </summary>
        private static IdentifiedMatchProbabilityRequest BuildSearchRequestMatchPredictionInput(ResultSet<MatchingAlgorithmResult> resultSet)
        {
            return new IdentifiedMatchProbabilityRequest
            {
                SearchRequestId = resultSet.SearchRequestId,
                MatchingAlgorithmHlaNomenclatureVersion = resultSet.MatchingAlgorithmHlaNomenclatureVersion,
                MatchingAlgorithmDataRefreshRecordId = resultSet.MatchingAlgorithmDataRefreshRecordId,
                // Passed on unresolved: match prediction resolves it against the kill-switch once per donor batch.
                UsePrecomputedGenotypeSets = resultSet.SearchRequest.UsePrecomputedGenotypeSets,
                ExcludedLoci = ExcludedLoci(resultSet.SearchRequest.MatchCriteria),
                PatientHla = resultSet.SearchRequest.SearchHlaData.ToPhenotypeInfo().ToPhenotypeInfoTransfer(),
                PatientFrequencySetMetadata = new FrequencySetMetadata
                {
                    EthnicityCode = resultSet.SearchRequest.PatientEthnicityCode,
                    RegistryCode = resultSet.SearchRequest.PatientRegistryCode
                }
            };
        }

        /// <summary>
        /// Pieces together various pieces of information into a match prediction input per donor.
        /// </summary>
        /// <returns>
        /// Match prediction input for the given search result.
        /// Null, if the donor's information could not be found in the donor store 
        /// </returns>
        private static DonorInput BuildPerDonorMatchPredictionInput(MatchingAlgorithmResult matchingAlgorithmResult) => new()
            {
                DonorId = matchingAlgorithmResult.AtlasDonorId,
                DonorHla = matchingAlgorithmResult.MatchingResult.DonorHla,
                DonorFrequencySetMetadata = new FrequencySetMetadata
                {
                    EthnicityCode = matchingAlgorithmResult.MatchingDonorInfo.EthnicityCode,
                    RegistryCode = matchingAlgorithmResult.MatchingDonorInfo.RegistryCode
                }
            };

        /// <summary>
        /// If a locus did not have match criteria provided, we do not want to calculate match probabilities at that locus.
        /// </summary>
        private static IEnumerable<Locus> ExcludedLoci(MismatchCriteria mismatchCriteria) =>
            EnumExtensions.EnumerateValues<Locus>().Where(l => mismatchCriteria.LocusMismatchCriteria.ToLociInfo().GetLocus(l) == null);
    }
}