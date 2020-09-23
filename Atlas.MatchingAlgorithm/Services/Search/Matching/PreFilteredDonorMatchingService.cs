using Atlas.Common.GeneticData;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Common.Models.SearchResults;
using Atlas.MatchingAlgorithm.Data.Repositories;
using Atlas.MatchingAlgorithm.Data.Models.SearchResults;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorRetrieval;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Atlas.MatchingAlgorithm.Services.Search.Matching
{
    public interface IPreFilteredDonorMatchingService
    {
        Task<IDictionary<int, MatchResult>> FindMatchesForLociFromDonorSelection(
            AlleleLevelMatchCriteria criteria,
            ICollection<Locus> loci,
            IDictionary<int, MatchResult> phaseOneMatchResults
        );
    }

    public class PreFilteredDonorMatchingService : DonorMatchingServiceBase, IPreFilteredDonorMatchingService
    {
        private readonly IPreFilteredDonorSearchRepository preFilteredDonorSearchRepository;
        private readonly IMatchFilteringService matchFilteringService;
        private readonly IPGroupRepository pGroupRepository;

        public PreFilteredDonorMatchingService(
            IActiveRepositoryFactory repositoryFactory,
            IMatchFilteringService matchFilteringService
        )
        {
            preFilteredDonorSearchRepository = repositoryFactory.GetPreFilteredDonorSearchRepository();
            pGroupRepository = repositoryFactory.GetPGroupRepository();
            this.matchFilteringService = matchFilteringService;
        }

        public async Task<IDictionary<int, MatchResult>> FindMatchesForLociFromDonorSelection(
            AlleleLevelMatchCriteria criteria,
            ICollection<Locus> loci,
            IDictionary<int, MatchResult> phaseOneMatchResults
        )
        {
            foreach (var locus in loci)
            {
                var results = await FindMatchesAtLocusFromDonorSelection(
                    criteria.SearchType,
                    locus,
                    criteria.LocusCriteria.GetLocus(locus),
                    phaseOneMatchResults.Keys
                );

                foreach (var (donorId, phase2Match) in results)
                {
                    var locusMatchDetails = phase2Match != null
                        ? phase2Match.Match
                        : new LocusMatchDetails {MatchCount = 0};

                    var phase1Match = phaseOneMatchResults[donorId];
                    phase1Match?.SetMatchDetailsForLocus(locus, locusMatchDetails);
                }

                var mismatchDonorIds = phaseOneMatchResults.Keys.Except(results.Select(r => r.Key));
                foreach (var mismatchDonorId in mismatchDonorIds)
                {
                    phaseOneMatchResults[mismatchDonorId].SetMatchDetailsForLocus(locus, new LocusMatchDetails {MatchCount = 0});
                }
            }

            return phaseOneMatchResults
                .Where(m => matchFilteringService.FulfilsTotalMatchCriteria(m.Value, criteria))
                .Where(m => loci.All(l => matchFilteringService.FulfilsPerLocusMatchCriteria(m.Value, criteria, l)))
                .ToDictionary(m => m.Key, m => m.Value);
        }

        private async Task<IDictionary<int, DonorAndMatchForLocus>> FindMatchesAtLocusFromDonorSelection(
            DonorType searchType,
            Locus locus,
            AlleleLevelLocusMatchCriteria criteria,
            IEnumerable<int> donorIds
        )
        {
            var repoCriteria = new LocusSearchCriteria
            {
                SearchDonorType = searchType,
                PGroupIdsToMatchInPositionOne = await pGroupRepository.GetPGroupIds(criteria.PGroupsToMatchInPositionOne),
                PGroupIdsToMatchInPositionTwo = await pGroupRepository.GetPGroupIds(criteria.PGroupsToMatchInPositionTwo),
                MismatchCount = criteria.MismatchCount,
            };

            var matches = (await preFilteredDonorSearchRepository.GetDonorMatchesAtLocusFromDonorSelection(locus, repoCriteria, donorIds))
                .GroupBy(m => m.DonorId)
                .ToDictionary(g => g.Key, g => DonorAndMatchFromGroup(g, locus));

            return matches;
        }
    }
}