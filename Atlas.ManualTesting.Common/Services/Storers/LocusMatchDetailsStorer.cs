using Atlas.Client.Models.Search.Results;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.ManualTesting.Common.Models.Entities;
using Atlas.ManualTesting.Common.Repositories;
using Atlas.MatchPrediction.ExternalInterface;

namespace Atlas.ManualTesting.Common.Services.Storers
{
    public class MatchingLocusDetailsStorer : LocusMatchDetailsStorer<MatchingAlgorithmResult>
    {
        public MatchingLocusDetailsStorer(
            IProcessedResultsRepository<LocusMatchDetails> resultsRepository,
            IMatchedDonorsRepository matchedDonorsRepository)
            : base(resultsRepository, matchedDonorsRepository)
        {
        }
    }

    public class SearchLocusDetailsStorer : LocusMatchDetailsStorer<SearchResult>
    {
        public SearchLocusDetailsStorer(
            IProcessedResultsRepository<LocusMatchDetails> resultsRepository,
            IMatchedDonorsRepository matchedDonorsRepository)
            : base(resultsRepository, matchedDonorsRepository)
        {
        }
    }

    public abstract class LocusMatchDetailsStorer<TResult> : ResultsStorer<TResult, LocusMatchDetails> where TResult : Result
    {
        private readonly IMatchedDonorsRepository matchedDonorsRepository;

        protected LocusMatchDetailsStorer(
            IProcessedResultsRepository<LocusMatchDetails> resultsRepository,
            IMatchedDonorsRepository matchedDonorsRepository)
            : base(resultsRepository)
        {
            this.matchedDonorsRepository = matchedDonorsRepository;
        }

        /// <returns>Locus match counts greater than zero.</returns>
        protected override async Task<IEnumerable<LocusMatchDetails>> ProcessSingleSearchResult(int searchRequestRecordId, TResult result)
        {
            var matchedDonorId = await matchedDonorsRepository.GetMatchedDonorId(searchRequestRecordId, result.DonorCode);
            
            if (matchedDonorId == null)
            {
                throw new Exception($"Could not find matched donor record for donor code {result.DonorCode}.");
            }

            var lociResults = result.ScoringResult.ScoringResultsByLocus.ToLociInfo();

            return MatchPredictionStaticData.MatchPredictionLoci
                .Select(l => new LocusMatchDetails
                {
                    Locus = l,
                    MatchedDonor_Id = matchedDonorId.Value,
                    MatchCount = lociResults.GetLocus(l).MatchCount,
                    MatchGrade_1 = lociResults.GetLocus(l).ScoreDetailsAtPositionOne.MatchGrade,
                    MatchGrade_2 = lociResults.GetLocus(l).ScoreDetailsAtPositionTwo.MatchGrade,
                    MatchConfidence_1 = lociResults.GetLocus(l).ScoreDetailsAtPositionOne.MatchConfidence,
                    MatchConfidence_2 = lociResults.GetLocus(l).ScoreDetailsAtPositionTwo.MatchConfidence,
                    IsAntigenMatch_1 = lociResults.GetLocus(l).ScoreDetailsAtPositionOne.IsAntigenMatch,
                    IsAntigenMatch_2 = lociResults.GetLocus(l).ScoreDetailsAtPositionTwo.IsAntigenMatch,
                });
        }
    }
}