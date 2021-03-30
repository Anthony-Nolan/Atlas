using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results;
using Atlas.Client.Models.Search.Results.MatchPrediction;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.MatchPrediction.ExternalInterface;
using Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.Verification;
using Atlas.MatchPrediction.Test.Verification.Data.Repositories;

namespace Atlas.MatchPrediction.Test.Verification.Services.Verification.ResultsProcessing.Storers
{
    internal class MatchedProbabilitiesStorer : ResultsStorer<SearchResult, MatchProbability>
    {
        private readonly IMatchedDonorsRepository matchedDonorsRepository;

        public MatchedProbabilitiesStorer(
            IProcessedResultsRepository<MatchProbability> resultsRepository,
            IMatchedDonorsRepository matchedDonorsRepository)
                : base(resultsRepository)
        {
            this.matchedDonorsRepository = matchedDonorsRepository;
        }

        protected override async Task<IEnumerable<MatchProbability>> ProcessSingleSearchResult(int searchRequestRecordId, SearchResult result)
        {
            var matchedDonorId = await matchedDonorsRepository.GetMatchedDonorId(
                searchRequestRecordId, int.Parse(result.DonorCode));

            if (matchedDonorId == null)
            {
                throw new Exception($"Could not find matched donor record for donor code {result.DonorCode}.");
            }

            var probabilities = BuildMatchProbabilities(
                matchedDonorId.Value, null, result.MatchPredictionResult.MatchProbabilities);
            var locusProbabilities = BuildLocusMatchProbabilities(
                matchedDonorId.Value, result.MatchPredictionResult.MatchProbabilitiesPerLocusTransfer);

            return probabilities.Concat(locusProbabilities).ToList();
        }

        private static IEnumerable<MatchProbability> BuildMatchProbabilities(
            int matchedDonorId,
            Locus? locus,
            MatchProbabilities probabilities)
        {
            return new List<MatchProbability>
            {
                new MatchProbability
                {
                    MatchedDonor_Id = matchedDonorId,
                    Locus = locus,
                    MismatchCount = 0,
                    Probability = probabilities.ZeroMismatchProbability?.Decimal
                },
                new MatchProbability
                {
                    MatchedDonor_Id = matchedDonorId,
                    Locus = locus,
                    MismatchCount = 1,
                    Probability = probabilities.OneMismatchProbability?.Decimal
                },
                new MatchProbability
                {
                    MatchedDonor_Id = matchedDonorId,
                    Locus = locus,
                    MismatchCount = 2,
                    Probability = probabilities.TwoMismatchProbability?.Decimal
                },
            };
        }
        private static IEnumerable<MatchProbability> BuildLocusMatchProbabilities(
            int matchedDonorId,
            LociInfoTransfer<MatchProbabilityPerLocusResponse> matchProbabilitiesPerLocus)
        {
            var lociInfo = matchProbabilitiesPerLocus.ToLociInfo();
            return MatchPredictionStaticData.MatchPredictionLoci
                .SelectMany(l => BuildMatchProbabilities(matchedDonorId, l, lociInfo.GetLocus(l).MatchProbabilities));
        }
    }
}