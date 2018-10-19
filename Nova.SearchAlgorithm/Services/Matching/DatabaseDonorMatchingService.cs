using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Models.SearchResults;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Repositories.Donors;

namespace Nova.SearchAlgorithm.Services.Matching
{
    public interface IDatabaseDonorMatchingService
    {
        /// <summary>
        /// Searches the pre-processed matching data for matches at the specified loci
        /// Performs filtering against loci and total mismatch counts
        /// </summary>
        /// <returns>
        /// A collection of PotentialSearchResults, with donor id populated. MatchDetails will be populated only for the specified loci
        /// </returns>
        Task<IEnumerable<MatchResult>> FindMatchesForLoci(AlleleLevelMatchCriteria criteria, IList<Locus> loci);

        Task<IEnumerable<MatchResult>> FindMatchesForLociFromDonorSelection(
            AlleleLevelMatchCriteria criteria,
            IList<Locus> loci,
            IEnumerable<MatchResult> matchResults
        );
    }

    public class DatabaseDonorMatchingService : IDatabaseDonorMatchingService
    {
        private readonly IDonorSearchRepository donorSearchRepository;
        private readonly IMatchFilteringService matchFilteringService;

        public DatabaseDonorMatchingService(IDonorSearchRepository donorSearchRepository, IMatchFilteringService matchFilteringService)
        {
            this.donorSearchRepository = donorSearchRepository;
            this.matchFilteringService = matchFilteringService;
        }

        public async Task<IEnumerable<MatchResult>> FindMatchesForLoci(AlleleLevelMatchCriteria criteria, IList<Locus> loci)
        {
            if (loci.Contains(Locus.Dpb1) || loci.Contains(Locus.Dqb1) || loci.Contains(Locus.C))
            {
                // Currently the logic here is not advised for these loci
                // Donors can be untyped at these loci, which counts as a potential match
                // so a simple search of the database would return a huge number of donors. 
                // To avoid serialising that many results, we filter on these loci based on the results at other loci
                throw new NotImplementedException();
            }

            var results = await Task.WhenAll(loci.Select(l =>
                FindMatchesAtLocus(criteria.SearchType, criteria.RegistriesToSearch, l, criteria.MatchCriteriaForLocus(l))));

            var matches = results
                .SelectMany(r => r)
                .GroupBy(m => m.Key)
                .Select(matchesForDonor =>
                {
                    var donorId = matchesForDonor.Key;
                    var result = new MatchResult
                    {
                        DonorId = donorId,
                    };
                    foreach (var locus in loci)
                    {
                        var matchesAtLocus = matchesForDonor.FirstOrDefault(m => m.Value.Locus == locus);
                        var locusMatchDetails = matchesAtLocus.Value != null
                            ? matchesAtLocus.Value.Match
                            : new LocusMatchDetails {MatchCount = 0};
                        result.SetMatchDetailsForLocus(locus, locusMatchDetails);
                    }

                    return result;
                })
                .Where(m => matchFilteringService.FulfilsTotalMatchCriteria(m, criteria))
                .Where(m => loci.All(l => matchFilteringService.FulfilsPerLocusMatchCriteria(m, criteria, l)));

            return matches.ToList();
        }

        public async Task<IEnumerable<MatchResult>> FindMatchesForLociFromDonorSelection(
            AlleleLevelMatchCriteria criteria,
            IList<Locus> loci,
            IEnumerable<MatchResult> matchResults
        )
        {
            matchResults = matchResults.ToList();
            var donorIds = matchResults.Select(m => m.DonorId).ToList();

            foreach (var locus in loci)
            {
                var results = await FindMatchesAtLocusFromDonorSelection(
                    criteria.SearchType,
                    criteria.RegistriesToSearch,
                    locus,
                    criteria.MatchCriteriaForLocus(locus),
                    donorIds
                );

                foreach (var matchesAtLocus in results)
                {
                    var locusMatchDetails = matchesAtLocus.Value != null
                        ? matchesAtLocus.Value.Match
                        : new LocusMatchDetails {MatchCount = 0};

                    var matchResult = matchResults.FirstOrDefault(m => m.DonorId == matchesAtLocus.Key);
                    matchResult?.SetMatchDetailsForLocus(locus, locusMatchDetails);
                }

                var mismatchDonorIds = donorIds.Except(results.Select(r => r.Key));
                foreach (var mismatchDonorId in mismatchDonorIds)
                {
                    matchResults.Single(r => r.DonorId == mismatchDonorId).SetMatchDetailsForLocus(locus, new LocusMatchDetails {MatchCount = 0});
                }
            }

            return matchResults
                .Where(m => matchFilteringService.FulfilsTotalMatchCriteria(m, criteria))
                .Where(m => loci.All(l => matchFilteringService.FulfilsPerLocusMatchCriteria(m, criteria, l)))
                .ToList();
        }

        private async Task<IDictionary<int, DonorAndMatchForLocus>> FindMatchesAtLocus(
            DonorType searchType,
            IEnumerable<RegistryCode> registriesToSearch,
            Locus locus,
            AlleleLevelLocusMatchCriteria criteria
        )
        {
            var repoCriteria = new LocusSearchCriteria
            {
                SearchType = searchType,
                Registries = registriesToSearch,
                PGroupsToMatchInPositionOne = criteria.PGroupsToMatchInPositionOne,
                PGroupsToMatchInPositionTwo = criteria.PGroupsToMatchInPositionTwo,
            };

            var matches = (await donorSearchRepository.GetDonorMatchesAtLocus(locus, repoCriteria))
                .GroupBy(m => m.DonorId)
                .ToDictionary(g => g.Key, g => DonorAndMatchFromGroup(g, locus));

            return matches;
        }

        private async Task<IDictionary<int, DonorAndMatchForLocus>> FindMatchesAtLocusFromDonorSelection(
            DonorType searchType,
            IEnumerable<RegistryCode> registriesToSearch,
            Locus locus,
            AlleleLevelLocusMatchCriteria criteria,
            IEnumerable<int> donorIds
        )
        {
            var repoCriteria = new LocusSearchCriteria
            {
                SearchType = searchType,
                Registries = registriesToSearch,
                PGroupsToMatchInPositionOne = criteria.PGroupsToMatchInPositionOne,
                PGroupsToMatchInPositionTwo = criteria.PGroupsToMatchInPositionTwo,
            };

            var matches = (await donorSearchRepository.GetDonorMatchesAtLocusFromDonorSelection(locus, repoCriteria, donorIds))
                .GroupBy(m => m.DonorId)
                .ToDictionary(g => g.Key, g => DonorAndMatchFromGroup(g, locus));

            return matches;
        }

        private static DonorAndMatchForLocus DonorAndMatchFromGroup(IGrouping<int, PotentialHlaMatchRelation> group, Locus locus)
        {
            var donorId = group.Key;
            return new DonorAndMatchForLocus
            {
                DonorId = donorId,
                Match = new LocusMatchDetails
                {
                    MatchCount = DirectMatch(group.ToList()) || CrossMatch(group.ToList()) ? 2 : 1
                },
                Locus = locus
            };
        }

        private static bool DirectMatch(IList<PotentialHlaMatchRelation> matches)
        {
            return matches.Any(m => m.SearchTypePosition == TypePositions.One && m.MatchingTypePositions.HasFlag(TypePositions.One))
                   && matches.Any(m => m.SearchTypePosition == TypePositions.Two && m.MatchingTypePositions.HasFlag(TypePositions.Two));
        }

        private static bool CrossMatch(IList<PotentialHlaMatchRelation> matches)
        {
            return matches.Any(m => m.SearchTypePosition == TypePositions.One && m.MatchingTypePositions.HasFlag(TypePositions.Two))
                   && matches.Any(m => m.SearchTypePosition == TypePositions.Two && m.MatchingTypePositions.HasFlag(TypePositions.One));
        }

        private class DonorAndMatchForLocus
        {
            public LocusMatchDetails Match { get; set; }
            public int DonorId { get; set; }
            public Locus Locus { get; set; }
        }
    }
}