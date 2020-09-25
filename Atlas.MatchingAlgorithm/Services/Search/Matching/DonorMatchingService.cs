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

            List<MatchResult> results = null;

            foreach (var locusCriteria in lociMismatchCounts.OrderBy(c => c.Item2?.MismatchCount))
            {
                var (locus, c) = locusCriteria;
                var locusResults = await FindMatchesAtLocus(criteria.SearchType, locus, c);
                if (results == null)
                {
                    results = locusResults.Select(lr =>
                    {
                        var (donorId, matchDetails) = lr;
                        var result = new MatchResult {DonorId = donorId};
                        result.SetMatchDetailsForLocus(locus, matchDetails.Match);
                        return result;
                    }).ToList();
                }
                else
                {
                    foreach (var result in results)
                    {
                        var locusMatch = locusResults.GetValueOrDefault(result.DonorId)?.Match ?? new LocusMatchDetails {MatchCount = 0};
                        result.SetMatchDetailsForLocus(locus, locusMatch);
                    }
                }
            }

            return results?
                .Where(m => loci.All(l => matchFilteringService.FulfilsPerLocusMatchCriteria(m, criteria, l)))
                .Where(m => matchFilteringService.FulfilsTotalMatchCriteria(m, criteria))
                .ToDictionary(m => m.DonorId, m => m);
        }

        private async Task<IDictionary<int, DonorAndMatchForLocus>> FindMatchesAtLocus(
            DonorType searchType,
            Locus locus,
            AlleleLevelLocusMatchCriteria criteria
        )
        {
            using (searchLogger.RunTimed($"{LoggingPrefix}Fetched donors from database - for Locus {locus}"))
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
                };

                var stopwatch = new Stopwatch();
                stopwatch.Start();

                var matchesAtLocus = await donorSearchRepository.GetDonorMatchesAtLocus(locus, repoCriteria, filteringOptions);

                searchLogger.SendTrace($"{LoggingPrefix}SQL Requests, {stopwatch.ElapsedMilliseconds}");
                stopwatch.Restart();

                var matches = matchesAtLocus
                    .GroupBy(m => m.DonorId)
                    .ToDictionary(g => g.Key, g => DonorAndMatchFromGroup(g, locus));

                searchLogger.SendTrace($"{LoggingPrefix}Direct/Cross analysis, {stopwatch.ElapsedMilliseconds}");
                return matches;
            }
        }
    }
}