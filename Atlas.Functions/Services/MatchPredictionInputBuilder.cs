using System.Collections.Generic;
using System.Linq;
using Atlas.Client.Models.Common.Requests;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.ResultSet;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.Functions.Settings;
using Atlas.MatchPrediction.ExternalInterface;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using EnumStringValues;
using Microsoft.Extensions.Options;

namespace Atlas.Functions.Services
{
    public interface IMatchPredictionInputBuilder
    {
        IEnumerable<MultipleDonorMatchProbabilityInput> BuildMatchPredictionInputs(ResultSet<MatchingAlgorithmResult> matchingResultSet);
    }

    internal class MatchPredictionInputBuilder : IMatchPredictionInputBuilder
    {
        private readonly ILogger logger;
        private readonly IDonorInputBatcher donorInputBatcher;
        private readonly int matchPredictionBatchSize;

        public MatchPredictionInputBuilder(
            ISearchLogger<SearchLoggingContext> logger,
            IDonorInputBatcher donorInputBatcher,
            IOptions<OrchestrationSettings> orchestrationSettings)
        {
            this.logger = logger;
            this.donorInputBatcher = donorInputBatcher;
            matchPredictionBatchSize = orchestrationSettings.Value.MatchPredictionBatchSize;
        }

        /// <inheritdoc />
        public IEnumerable<MultipleDonorMatchProbabilityInput> BuildMatchPredictionInputs(ResultSet<MatchingAlgorithmResult> matchingResultSet)
        {
            using (logger.RunTimed($"Building match prediction inputs: {matchingResultSet.SearchRequestId}"))
            {
                var nonDonorInput = BuildSearchRequestMatchPredictionInput(matchingResultSet);
                var donorInputs = matchingResultSet.Results.Select(BuildPerDonorMatchPredictionInput);

                return donorInputBatcher.BatchDonorInputs(nonDonorInput, donorInputs, matchPredictionBatchSize).ToList();
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