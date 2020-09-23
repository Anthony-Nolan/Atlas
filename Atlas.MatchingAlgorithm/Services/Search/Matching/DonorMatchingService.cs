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
            var matchFindingTasks = loci.Select(l => FindMatchesAtLocus(criteria.SearchType, l, criteria.LocusCriteria.GetLocus(l))).ToList();
            var results = await Task.WhenAll(matchFindingTasks);

            searchLogger.SendTrace($"MATCHING PHASE1: all donors from Db. {results.Sum(x => x.Count)} results in {stopwatch.ElapsedMilliseconds} ms");
            stopwatch.Restart();

            var matches = results
                .SelectMany(r => r)
                .GroupBy(m => m.Key)
                // If no mismatches are allowed - donors must be matched at all provided loci. This check performed upfront to improve performance of such searches 
                .Where(g => criteria.DonorMismatchCount != 0 || g.Count() == loci.Count)
                .Select(matchesForDonor =>
                {
                    var donorId = matchesForDonor.Key;
                    var result = new MatchResult {DonorId = donorId};
                    foreach (var locus in loci)
                    {
                        var (_, donorAndMatchForLocus) = matchesForDonor.FirstOrDefault(m => m.Value.Locus == locus);
                        var locusMatchDetails = donorAndMatchForLocus != null
                            ? donorAndMatchForLocus.Match
                            : new LocusMatchDetails {MatchCount = 0};
                        result.SetMatchDetailsForLocus(locus, locusMatchDetails);
                    }

                    return result;
                })
                .Where(m => loci.All(l => matchFilteringService.FulfilsPerLocusMatchCriteria(m, criteria, l)))
                .Where(m => matchFilteringService.FulfilsTotalMatchCriteria(m, criteria))
                .ToList();

            searchLogger.SendTrace($"MATCHING PHASE1: Manipulate + filter: {stopwatch.ElapsedMilliseconds}");
            stopwatch.Restart();

            return matches.ToDictionary(m => m.DonorId, m => m);
        }

        private async Task<IDictionary<int, DonorAndMatchForLocus>> FindMatchesAtLocus(
            DonorType searchType,
            Locus locus,
            AlleleLevelLocusMatchCriteria criteria
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
            };

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var matchesAtLocus = await donorSearchRepository.GetDonorMatchesAtLocus(locus, repoCriteria, filteringOptions);
            searchLogger.SendTrace($"MATCHING PHASE1: SQL Requests, {stopwatch.ElapsedMilliseconds}");
            stopwatch.Restart();

            var matches = matchesAtLocus
                .GroupBy(m => m.DonorId)
                .ToDictionary(g => g.Key, g => DonorAndMatchFromGroup(g, locus));

            searchLogger.SendTrace($"MATCHING PHASE1: Direct/Cross analysis, {stopwatch.ElapsedMilliseconds}");

            return matches;
        }
    }
}