using Atlas.Common.ApplicationInsights;
using Atlas.Common.GeneticData;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Common.Config;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Common.Models.Matching;
using Atlas.MatchingAlgorithm.Common.Models.SearchResults;
using Atlas.MatchingAlgorithm.Data.Repositories;
using Atlas.MatchingAlgorithm.Data.Models.SearchResults;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorRetrieval;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Data.Models;

namespace Atlas.MatchingAlgorithm.Services.Search.Matching
{
    public interface IDonorMatchingService
    {
        /// <summary>
        /// Searches the pre-processed matching data for matches at the specified loci.
        /// Performs filtering against loci and total mismatch counts.
        /// </summary>
        /// <returns>
        /// A dictionary of PotentialSearchResults, keyed by donor id.
        /// MatchDetails will be populated only for the specified loci.
        /// </returns>
        Task<IDictionary<int, MatchResult>> FindMatchesForLoci(AlleleLevelMatchCriteria criteria, ICollection<Locus> loci);
    }

    public class DonorMatchingService : DonorMatchingServiceBase, IDonorMatchingService
    {
        private const string LoggingPrefix = "MATCHING PHASE1: ";

        private readonly IDonorSearchRepository donorSearchRepository;
        private readonly IMatchFilteringService matchFilteringService;
        private readonly IDatabaseFilteringAnalyser databaseFilteringAnalyser;
        private readonly ILogger searchLogger;
        private readonly IPGroupRepository pGroupRepository;

        public DonorMatchingService(
            IActiveRepositoryFactory repositoryFactory,
            IMatchFilteringService matchFilteringService,
            IDatabaseFilteringAnalyser databaseFilteringAnalyser,
            // ReSharper disable once SuggestBaseTypeForParameter
            IMatchingAlgorithmSearchLogger searchLogger
        )
        {
            donorSearchRepository = repositoryFactory.GetDonorSearchRepository();
            pGroupRepository = repositoryFactory.GetPGroupRepository();
            this.matchFilteringService = matchFilteringService;
            this.databaseFilteringAnalyser = databaseFilteringAnalyser;
            this.searchLogger = searchLogger;
        }

        public async Task<IDictionary<int, MatchResult>> FindMatchesForLoci(AlleleLevelMatchCriteria criteria, ICollection<Locus> loci)
        {
            if (loci.Any(locus => !LocusSettings.LociPossibleToMatchInMatchingPhaseOne.Contains(locus)))
            {
                // Currently the logic here is not advised for these loci
                // Donors can be untyped at these loci, which counts as a potential match
                // so a simple search of the database would return a huge number of donors. 
                // To avoid serialising that many results, we filter on these loci based on the results at other loci
                throw new NotImplementedException();
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var lociMismatchCounts = loci.Select(l => (l, criteria.LocusCriteria.GetLocus(l))).ToList();
            if (lociMismatchCounts.All(c => c.Item2?.MismatchCount == 2))
            {
                throw new NotImplementedException("TODO: ATLAS-714: Re-work out how to do 4/8");
            }

            // Keyed by DonorId
            IDictionary<int, MatchResult> results = new Dictionary<int, MatchResult>();
            var matchedLoci = new HashSet<Locus>();

            foreach (var locusCriteria in lociMismatchCounts.OrderBy(c => c.Item2?.MismatchCount))
            {
                var (locus, c) = locusCriteria;
                var donorIds = !matchedLoci.Any() ? null : results.Keys.ToHashSet();
                var locusResults = await FindMatchesAtLocus(criteria.SearchType, locus, c, donorIds);

                await foreach (var locusResult in locusResults)
                {
                    var donorId = locusResult.DonorId;
                    var result = results.GetValueOrDefault(donorId);
                    if (result == default)
                    {
                        result = new MatchResult {DonorId = donorId};
                        results[donorId] = result;
                    }

                    result.UpdatePositionPairsForLocus(locus, locusResult.SearchTypePosition, locusResult.MatchingTypePosition);
                }

                matchedLoci.Add(locus);
                results = results
                    .Where(m => matchedLoci.All(l => matchFilteringService.FulfilsPerLocusMatchCriteria(m.Value, criteria, l)))
                    .ToDictionary();
            }

            return results?
                // .Where(m => loci.All(l => matchFilteringService.FulfilsPerLocusMatchCriteria(m.Value, criteria, l)))
                .Where(m => matchFilteringService.FulfilsTotalMatchCriteria(m.Value, criteria))
                .ToDictionary();
        }

        private async Task<IAsyncEnumerable<PotentialHlaMatchRelation>> FindMatchesAtLocus(
            DonorType searchType,
            Locus locus,
            AlleleLevelLocusMatchCriteria criteria,
            HashSet<int> donorIds = null
        )
        {
            var repoCriteria = new LocusSearchCriteria
            {
                SearchDonorType = searchType,
                PGroupIdsToMatchInPositionOne = await pGroupRepository.GetPGroupIds(criteria.PGroupsToMatchInPositionOne),
                PGroupIdsToMatchInPositionTwo = await pGroupRepository.GetPGroupIds(criteria.PGroupsToMatchInPositionTwo),
                MismatchCount = criteria.MismatchCount,
            };

            var filteringOptions = new MatchingFilteringOptions
            {
                ShouldFilterOnDonorType = databaseFilteringAnalyser.ShouldFilterOnDonorTypeInDatabase(repoCriteria),
                DonorIds = donorIds
            };

            return donorSearchRepository.GetDonorMatchesAtLocus(locus, repoCriteria, filteringOptions);
        }
    }
}