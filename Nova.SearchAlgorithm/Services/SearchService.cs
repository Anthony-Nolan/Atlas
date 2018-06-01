using System;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Data.Repositories;
using Nova.SearchAlgorithm.Data.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Scoring;

namespace Nova.SearchAlgorithm.Services
{
    public interface ISearchService
    {
        Task<IEnumerable<PotentialMatch>> Search(SearchRequest searchRequest);
    }

    public class SearchService : ISearchService
    {
        private readonly IDonorSearchRepository donorRepository;
        private readonly IMatchingDictionaryLookupService lookupService;
        private readonly ICalculateScore calculateScore;

        public SearchService(IDonorSearchRepository donorRepository, IMatchingDictionaryLookupService lookupService, ICalculateScore calculateScore)
        {
            this.donorRepository = donorRepository;
            this.lookupService = lookupService;
            this.calculateScore = calculateScore;
        }

        public async Task<IEnumerable<PotentialMatch>> Search(SearchRequest searchRequest)
        {
            DonorMatchCriteria criteria = new DonorMatchCriteria
            {
                SearchType = searchRequest.SearchType,
                RegistriesToSearch = searchRequest.RegistriesToSearch,
                DonorMismatchCount = searchRequest.MatchCriteria.DonorMismatchCount,
                LocusMismatchA = await MapMismatchToMatchCriteria(Locus.A, searchRequest.MatchCriteria.LocusMismatchA),
                LocusMismatchB = await MapMismatchToMatchCriteria(Locus.B, searchRequest.MatchCriteria.LocusMismatchB),
                LocusMismatchC = await MapMismatchToMatchCriteria(Locus.C, searchRequest.MatchCriteria.LocusMismatchC),
                LocusMismatchDRB1 = await MapMismatchToMatchCriteria(Locus.Drb1, searchRequest.MatchCriteria.LocusMismatchDRB1),
                LocusMismatchDQB1 = await MapMismatchToMatchCriteria(Locus.Dqb1, searchRequest.MatchCriteria.LocusMismatchDQB1),
            };

            var threeLociMatches = donorRepository.Search(criteria);

            var fiveLociMatches = threeLociMatches.Select(AddMatchCounts(criteria)).Where(FilterByMismatchCriteria(criteria));

            var scoredMatches = await Task.WhenAll(fiveLociMatches.Select(calculateScore.Score));

            return scoredMatches;
        }

        private async Task<DonorLocusMatchCriteria> MapMismatchToMatchCriteria(Locus locus, LocusMismatchCriteria mismatch)
        {
            if (mismatch == null)
            {
                return null;
            }

            var hla1 = lookupService.GetMatchingHla(locus.ToMatchLocus(), mismatch.SearchHla1);
            var hla2 = lookupService.GetMatchingHla(locus.ToMatchLocus(), mismatch.SearchHla2);

            return new DonorLocusMatchCriteria
            {
                MismatchCount = mismatch.MismatchCount,
                HlaNamesToMatchInPositionOne = (await hla1).MatchingPGroups,
                HlaNamesToMatchInPositionTwo = (await hla2).MatchingPGroups,
            };
        }

        private Func<PotentialMatch, PotentialMatch> AddMatchCounts(DonorMatchCriteria criteria)
        {
            // TODO:NOVA-1289 (create tests and) add match counts based on C and DBQR
            return m => m;
        }

        private Func<PotentialMatch, bool> FilterByMismatchCriteria(DonorMatchCriteria criteria)
        {
            // TODO:NOVA-1289 (create tests and) filter based on total match count and all 5 loci match counts
            return m => true;
        }
    }
}