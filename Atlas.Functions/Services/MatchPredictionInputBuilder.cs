using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Client.Models.Search.Requests;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.DonorImport.ExternalInterface.Models;
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
        public MatchingAlgorithmResultSet MatchingAlgorithmResults { get; set; }
        public IReadOnlyDictionary<int, Donor> DonorDictionary { get; set; }
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
            ILogger logger,
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
                var donorDictionary = matchPredictionInputParameters.DonorDictionary;
                var nonDonorInput = BuildSearchRequestMatchPredictionInput(
                    matchingAlgorithmResultSet.SearchRequestId,
                    searchRequest,
                    matchingAlgorithmResultSet.HlaNomenclatureVersion
                );
                var donorInputs = matchingAlgorithmResultSet.MatchingAlgorithmResults.Select(matchingResult => BuildPerDonorMatchPredictionInput(
                        matchingResult,
                        donorDictionary
                    ))
                    .Where(r => r != null);
                return donorInputBatcher.BatchDonorInputs(nonDonorInput, donorInputs, matchPredictionBatchSize);
            }
        }

        /// <summary>
        /// Builds all non-donor information required to run the match prediction algorithm for a search request.
        /// e.g. patient info, hla nomenclature, matching preferences
        /// 
        /// This will remain constant for all donors in the request, so only needs to be calculated once.
        /// </summary>
        /// <param name="searchRequestId"></param>
        /// <param name="searchRequest"></param>
        /// <param name="hlaNomenclatureVersion"></param>
        /// <returns></returns>
        private static MatchProbabilityRequestInput BuildSearchRequestMatchPredictionInput(
            string searchRequestId,
            SearchRequest searchRequest,
            string hlaNomenclatureVersion
        )
        {
            var batchId = Guid.NewGuid().ToString();
            return new MatchProbabilityRequestInput(batchId)
            {
                SearchRequestId = searchRequestId,
                ExcludedLoci = ExcludedLoci(searchRequest.MatchCriteria),
                PatientHla = searchRequest.SearchHlaData.ToPhenotypeInfo().ToPhenotypeInfoTransfer(),
                PatientFrequencySetMetadata = new FrequencySetMetadata
                {
                    EthnicityCode = searchRequest.PatientEthnicityCode,
                    RegistryCode = searchRequest.PatientRegistryCode
                },
                HlaNomenclatureVersion = hlaNomenclatureVersion
            };
        }

        /// <summary>
        /// Pieces together various pieces of information into a match prediction input per donor.
        /// </summary>
        /// <returns>
        /// Match prediction input for the given search result.
        /// Null, if the donor's information could not be found in the donor store 
        /// </returns>
        private DonorInput BuildPerDonorMatchPredictionInput(
            MatchingAlgorithmResult matchingAlgorithmResult,
            IReadOnlyDictionary<int, Donor> donorDictionary)
        {
            if (!donorDictionary.TryGetValue(matchingAlgorithmResult.AtlasDonorId, out var donorInfo))
            {
                var message = @$"Could not fetch donor information needed for match prediction for donor: {matchingAlgorithmResult.AtlasDonorId}. 
                                        It is possible that this donor was removed between matching completing and match prediction initiation.";
                logger.SendTrace(message);
                return null;
            }

            return new DonorInput
            {
                DonorId = matchingAlgorithmResult.AtlasDonorId,
                DonorHla = matchingAlgorithmResult.MatchingResult.DonorHla,
                DonorFrequencySetMetadata = new FrequencySetMetadata
                {
                    EthnicityCode = donorInfo.EthnicityCode,
                    RegistryCode = donorInfo.RegistryCode
                },
            };
        }

        /// <summary>
        /// If a locus did not have match criteria provided, we do not want to calculate match probabilities at that locus.
        /// </summary>
        private static IEnumerable<Locus> ExcludedLoci(MismatchCriteria mismatchCriteria) =>
            EnumExtensions.EnumerateValues<Locus>().Where(l => mismatchCriteria.LocusMismatchCriteria.ToLociInfo().GetLocus(l) == null);
    }
}