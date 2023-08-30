using System.Collections.Generic;
using System.Linq;
using Atlas.Client.Models.Search.Requests;
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
    // ReSharper disable once ClassNeverInstantiated.Global - Used in activity function
    /// <summary>
    /// Parameters wrapped in single object as Azure Activity functions may only have one parameter.
    /// </summary>
    public class MatchPredictionInputParameters
    {
        public SearchRequest SearchRequest { get; set; }
        public ResultSet<MatchingAlgorithmResult> MatchingAlgorithmResults { get; set; }
    }

    public interface IMatchPredictionInputBuilder
    {
        IEnumerable<MultipleDonorMatchProbabilityInput> BuildMatchPredictionInputs(MatchPredictionInputParameters matchPredictionInputParameters);
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
        public IEnumerable<MultipleDonorMatchProbabilityInput> BuildMatchPredictionInputs(
            MatchPredictionInputParameters matchPredictionInputParameters)
        {
            using (logger.RunTimed($"Building match prediction inputs: {matchPredictionInputParameters.MatchingAlgorithmResults.SearchRequestId}"))
            {
                var matchingAlgorithmResultSet = matchPredictionInputParameters.MatchingAlgorithmResults;
                var searchRequest = matchPredictionInputParameters.SearchRequest;
                var nonDonorInput = BuildSearchRequestMatchPredictionInput(
                    matchingAlgorithmResultSet.SearchRequestId,
                    searchRequest
                );
                var donorInputs = matchingAlgorithmResultSet.Results.Select(BuildPerDonorMatchPredictionInput);

                return donorInputBatcher.BatchDonorInputs(nonDonorInput, donorInputs, matchPredictionBatchSize).ToList();
            }
        }

        /// <summary>
        /// Builds all non-donor information required to run the match prediction algorithm for a search request.
        /// e.g. patient info, matching preferences
        /// 
        /// This will remain constant for all donors in the request, so only needs to be calculated once.
        /// </summary>
        /// <param name="searchRequestId"></param>
        /// <param name="searchRequest"></param>
        /// <returns></returns>
        private static IdentifiedMatchProbabilityRequest BuildSearchRequestMatchPredictionInput(
            string searchRequestId,
            SearchRequest searchRequest
        )
        {
            return new IdentifiedMatchProbabilityRequest
            {
                SearchRequestId = searchRequestId,
                ExcludedLoci = ExcludedLoci(searchRequest.MatchCriteria),
                PatientHla = searchRequest.SearchHlaData.ToPhenotypeInfo().ToPhenotypeInfoTransfer(),
                PatientFrequencySetMetadata = new FrequencySetMetadata
                {
                    EthnicityCode = searchRequest.PatientEthnicityCode,
                    RegistryCode = searchRequest.PatientRegistryCode
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
        private DonorInput BuildPerDonorMatchPredictionInput(MatchingAlgorithmResult matchingAlgorithmResult) =>
            new DonorInput
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