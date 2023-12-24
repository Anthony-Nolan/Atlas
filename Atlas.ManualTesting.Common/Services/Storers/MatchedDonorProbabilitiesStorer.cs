using Atlas.Client.Models.Search.Results;
using Atlas.Client.Models.Search.Results.MatchPrediction;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.ManualTesting.Common.Models.Entities;
using Atlas.ManualTesting.Common.Repositories;
using Atlas.MatchPrediction.ExternalInterface;

namespace Atlas.ManualTesting.Common.Services.Storers
{
    public class MatchedDonorProbabilitiesStorer : ResultsStorer<SearchResult, MatchedDonorProbability>
    {
        private readonly IMatchedDonorsRepository matchedDonorsRepository;

        public MatchedDonorProbabilitiesStorer(
            IProcessedResultsRepository<MatchedDonorProbability> resultsRepository,
            IMatchedDonorsRepository matchedDonorsRepository)
                : base(resultsRepository)
        {
            this.matchedDonorsRepository = matchedDonorsRepository;
        }

        protected override async Task<IEnumerable<MatchedDonorProbability>> ProcessSingleSearchResult(int searchRequestRecordId, SearchResult result)
        {
            var matchedDonorId = await matchedDonorsRepository.GetMatchedDonorId(searchRequestRecordId, result.DonorCode);

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

        private static IEnumerable<MatchedDonorProbability> BuildMatchProbabilities(
            int matchedDonorId,
            Locus? locus,
            MatchProbabilities probabilities)
        {
            return new List<MatchedDonorProbability>
            {
                new()
                {
                    MatchedDonor_Id = matchedDonorId,
                    Locus = locus,
                    MismatchCount = 0,
                    Probability = probabilities.ZeroMismatchProbability?.Decimal,
                    ProbabilityAsPercentage = probabilities.ZeroMismatchProbability?.Percentage
                },
                new()
                {
                    MatchedDonor_Id = matchedDonorId,
                    Locus = locus,
                    MismatchCount = 1,
                    Probability = probabilities.OneMismatchProbability?.Decimal,
                    ProbabilityAsPercentage = probabilities.OneMismatchProbability?.Percentage
                },
                new()
                {
                    MatchedDonor_Id = matchedDonorId,
                    Locus = locus,
                    MismatchCount = 2,
                    Probability = probabilities.TwoMismatchProbability?.Decimal,
                    ProbabilityAsPercentage = probabilities.TwoMismatchProbability?.Percentage
                },
            };
        }
        private static IEnumerable<MatchedDonorProbability> BuildLocusMatchProbabilities(
            int matchedDonorId,
            LociInfoTransfer<MatchProbabilityPerLocusResponse> matchProbabilitiesPerLocus)
        {
            var lociInfo = matchProbabilitiesPerLocus.ToLociInfo();
            return MatchPredictionStaticData.MatchPredictionLoci
                .SelectMany(l => BuildMatchProbabilities(matchedDonorId, l, lociInfo.GetLocus(l).MatchProbabilities));
        }
    }
}