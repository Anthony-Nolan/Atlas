using Atlas.Common.ApplicationInsights;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.Functions.Models.Search.Requests;
using Atlas.MatchingAlgorithm.Client.Models.SearchResults;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using System.Collections.Generic;
using System.Linq;

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
        public Dictionary<int, Donor> DonorDictionary { get; set; }
    }

    public interface IMatchPredictionInputBuilder
    {
        IEnumerable<MatchProbabilityInput> BuildMatchPredictionInputs(MatchPredictionInputParameters matchPredictionInputParameters);
    }

    internal class MatchPredictionInputBuilder : IMatchPredictionInputBuilder
    {
        private readonly ILogger logger;

        public MatchPredictionInputBuilder(ILogger logger)
        {
            this.logger = logger;
        }

        /// <inheritdoc />
        public IEnumerable<MatchProbabilityInput> BuildMatchPredictionInputs(MatchPredictionInputParameters matchPredictionInputParameters)
        {
            var matchingAlgorithmResultSet = matchPredictionInputParameters.MatchingAlgorithmResults;
            var searchRequest = matchPredictionInputParameters.SearchRequest;
            var donorDictionary = matchPredictionInputParameters.DonorDictionary;

            return matchingAlgorithmResultSet.MatchingAlgorithmResults.Select(matchingResult => BuildMatchPredictionInput(
                    matchingResult,
                    searchRequest,
                    donorDictionary,
                    matchingAlgorithmResultSet.HlaNomenclatureVersion,
                    matchingAlgorithmResultSet.SearchRequestId
                ))
                .Where(r => r != null);
        }

        /// <summary>
        /// Pieces together various pieces of information into a match prediction input
        /// </summary>
        /// <returns>
        /// Match prediction input for the given search result.
        /// Null, if the donor's information could not be found in the donor store 
        /// </returns>
        private MatchProbabilityInput BuildMatchPredictionInput(
            MatchingAlgorithmResult matchingAlgorithmResult,
            SearchRequest searchRequest,
            IReadOnlyDictionary<int, Donor> donorDictionary,
            string hlaNomenclatureVersion,
            string searchRequestId)
        {
            if (!donorDictionary.TryGetValue(matchingAlgorithmResult.AtlasDonorId, out var donorInfo))
            {
                var message = @$"Could not fetch donor information needed for match prediction for donor: {matchingAlgorithmResult.AtlasDonorId}. 
                                        It is possible that this donor was removed between matching completing and match prediction initiation.";
                logger.SendTrace(message);
                return null;
            }

            return new MatchProbabilityInput
            {
                SearchRequestId = searchRequestId,
                DonorId = matchingAlgorithmResult.AtlasDonorId,
                DonorHla = ExcludeHla(matchingAlgorithmResult.DonorHla, searchRequest.MatchCriteria),
                DonorFrequencySetMetadata = new FrequencySetMetadata
                {
                    EthnicityCode = donorInfo.EthnicityCode,
                    RegistryCode = donorInfo.RegistryCode
                },
                PatientHla = ExcludeHla(searchRequest.SearchHlaData.ToPhenotypeInfo(), searchRequest.MatchCriteria),
                PatientFrequencySetMetadata = new FrequencySetMetadata
                {
                    EthnicityCode = searchRequest.PatientEthnicityCode,
                    RegistryCode = searchRequest.PatientRegistryCode
                },
                HlaNomenclatureVersion = hlaNomenclatureVersion
            };
        }

        /// <summary>
        /// If a locus did not have match criteria provided, we do not want to calculate match probabilities at that locus -
        /// so we set the patient and donor hla to null for the purposes of match prediction 
        /// </summary>
        private static PhenotypeInfo<string> ExcludeHla(PhenotypeInfo<string> hla, MismatchCriteria mismatchCriteria)
        {
            return hla.Map((locus, data) => mismatchCriteria.MismatchCriteriaAtLocus(locus) == null ? null : data);
        }
    }
}