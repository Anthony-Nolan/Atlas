using System;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Data.Repositories;
using Nova.SearchAlgorithm.Data.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.MatchingDictionaryConversions;
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
            var criteriaMappings = await Task.WhenAll(
                MapMismatchToMatchCriteria(Locus.A, searchRequest.MatchCriteria.LocusMismatchA),
                MapMismatchToMatchCriteria(Locus.B, searchRequest.MatchCriteria.LocusMismatchB),
                MapMismatchToMatchCriteria(Locus.C, searchRequest.MatchCriteria.LocusMismatchC),
                MapMismatchToMatchCriteria(Locus.Drb1, searchRequest.MatchCriteria.LocusMismatchDRB1),
                MapMismatchToMatchCriteria(Locus.Dqb1, searchRequest.MatchCriteria.LocusMismatchDQB1));

            var criteria = new AlleleLevelMatchCriteria
            {
                SearchType = searchRequest.SearchType,
                RegistriesToSearch = searchRequest.RegistriesToSearch,
                DonorMismatchCount = (int) searchRequest.MatchCriteria.DonorMismatchCount,
                LocusMismatchA = criteriaMappings[0],
                LocusMismatchB = criteriaMappings[1],
                LocusMismatchC = criteriaMappings[2],
                LocusMismatchDRB1 = criteriaMappings[3],
                LocusMismatchDQB1 = criteriaMappings[4]
            };

            var threeLociMatches = await donorRepository.Search(criteria);

            var fiveLociMatches = threeLociMatches
                .Select(AddMatchCounts(criteria))
                .Where(FilterByMismatchCriteria(criteria));

            // TODO:NOVA-930 this won't update total match grade and confidence, only per-locus
            var scoredMatches = await Task.WhenAll(fiveLociMatches.Select(m => calculateScore.Score(criteria, m)));

            return scoredMatches.Select(MapSearchResultToApiObject).OrderBy(r => r.MatchRank);
        }

        private async Task<AlleleLevelLocusMatchCriteria> MapMismatchToMatchCriteria(Locus locus, LocusMismatchCriteria mismatch)
        {
            if (mismatch == null)
            {
                return null;
            }

            var lookupResult = await Task.WhenAll(
                lookupService.GetMatchingHla(locus.ToMatchLocus(), mismatch.SearchHla1),
                lookupService.GetMatchingHla(locus.ToMatchLocus(), mismatch.SearchHla2));

            return new AlleleLevelLocusMatchCriteria
            {
                MismatchCount = mismatch.MismatchCount,
                HlaNamesToMatchInPositionOne = lookupResult[0].MatchingPGroups,
                HlaNamesToMatchInPositionTwo = lookupResult[1].MatchingPGroups,
            };
        }

        private Func<PotentialSearchResult, PotentialSearchResult> AddMatchCounts(AlleleLevelMatchCriteria criteria)
        {
            // TODO:NOVA-1289 (create tests and) add match counts based on C and DBQR
            // TODO:NOVA-1289 implement typed loci booleans and counts
            return potentialSearchResult =>
            {
                var donorHla = potentialSearchResult.Donor.MatchingHla;

                potentialSearchResult.MatchDetailsAtLocusC =
                    MatchDetails(criteria.LocusMismatchC, donorHla?.C_1, donorHla?.C_2);

                potentialSearchResult.MatchDetailsAtLocusDqb1 =
                    MatchDetails(criteria.LocusMismatchC, donorHla?.DQB1_1, donorHla?.DQB1_2);

                potentialSearchResult.TotalMatchCount += potentialSearchResult.MatchDetailsAtLocusDqb1.MatchCount;
                potentialSearchResult.TotalMatchCount += potentialSearchResult.MatchDetailsAtLocusDqb1.MatchCount;
                
                return potentialSearchResult;
            };
        }

        private LocusMatchDetails MatchDetails(AlleleLevelLocusMatchCriteria criteria, ExpandedHla hla1, ExpandedHla hla2)
        {
            var matchDetails = new LocusMatchDetails
            {
                MatchCount = 2, // Assume a match until we know otherwise
                IsLocusTyped = hla1 != null && hla2 != null,
            };

            if (criteria != null && hla1 != null && hla2 != null)
            {
                // We have typed search and donor hla to compare
                matchDetails.MatchCount = 0;

                // TODO:NOVA-1289 This sketch logic does not take into account some edge cases, like one patient position matching both donor positions,
                // which should count as only one match
                if (criteria.HlaNamesToMatchInPositionOne.Any(name =>
                    hla1.PGroups.Union(hla2.PGroups).Contains(name)))
                {
                    matchDetails.MatchCount += 1;
                }

                if (criteria.HlaNamesToMatchInPositionTwo.Any(name =>
                    hla1.PGroups.Union(hla2.PGroups).Contains(name)))
                {
                    matchDetails.MatchCount += 1;
                }
            }

            return matchDetails;
        }

        private Func<PotentialSearchResult, bool> FilterByMismatchCriteria(AlleleLevelMatchCriteria criteria)
        {
            // TODO:NOVA-1289 (create tests and) filter based on total match count and all 5 loci match counts
            return potentialSearchResult =>
            {
                if (potentialSearchResult.MatchDetailsAtLocusC != null &&
                    criteria.LocusMismatchC != null &&
                    potentialSearchResult.MatchDetailsAtLocusC.MatchCount < criteria.LocusMismatchC.MismatchCount)
                {
                    return false;
                }

                if (potentialSearchResult.MatchDetailsAtLocusDqb1 != null &&
                    criteria.LocusMismatchDQB1 != null &&
                    potentialSearchResult.MatchDetailsAtLocusDqb1.MatchCount < criteria.LocusMismatchDQB1.MismatchCount)
                {
                    return false;
                }

                // TODO:NOVA-1289 take into account cord or adult search differences
                if (potentialSearchResult.TotalMatchCount < criteria.DonorMismatchCount)
                {
                    return false;
                }

                return true;
            };
        }

        private PotentialMatch MapSearchResultToApiObject(PotentialSearchResult result)
        {
            return new PotentialMatch
            {
                DonorId = result.Donor.DonorId,
                DonorType = result.Donor.DonorType,
                Registry = result.Donor.RegistryCode,
                MatchRank = result.MatchRank,
                TotalMatchConfidence = result.TotalMatchConfidence,
                TotalMatchGrade = result.TotalMatchGrade,
                TotalMatchCount = result.TotalMatchCount,
                TypedLociCount = result.TypedLociCount,
                MatchDetailsAtLocusA = result.MatchDetailsAtLocusA,
                MatchDetailsAtLocusB = result.MatchDetailsAtLocusB,
                MatchDetailsAtLocusC = result.MatchDetailsAtLocusC,
                MatchDetailsAtLocusDQB1 = result.MatchDetailsAtLocusDqb1,
                MatchDetailsAtLocusDRB1 = result.MatchDetailsAtLocusDrb1
            };
        }
    }
}