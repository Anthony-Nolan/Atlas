using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Models.SearchResults;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using Nova.SearchAlgorithm.MatchingDictionaryConversions;

namespace Nova.SearchAlgorithm.Services.Matching
{
    public interface IDonorMatchingService
    {
        Task<IEnumerable<MatchResult>> GetMatches(AlleleLevelMatchCriteria criteria);
    }

    public class DonorMatchingService : IDonorMatchingService
    {
        private readonly IDatabaseDonorMatchingService databaseDonorMatchingService;
        private readonly IDonorMatchCalculator donorMatchCalculator;
        private readonly IDonorInspectionRepository donorInspectionRepository;
        private readonly IMatchFilteringService matchFilteringService;

        public DonorMatchingService(
            IDatabaseDonorMatchingService databaseDonorMatchingService,
            IDonorMatchCalculator donorMatchCalculator,
            IDonorInspectionRepository donorInspectionRepository,
            IMatchFilteringService matchFilteringService
        )
        {
            this.databaseDonorMatchingService = databaseDonorMatchingService;
            this.donorMatchCalculator = donorMatchCalculator;
            this.donorInspectionRepository = donorInspectionRepository;
            this.matchFilteringService = matchFilteringService;
        }

        public async Task<IEnumerable<MatchResult>> GetMatches(AlleleLevelMatchCriteria criteria)
        {
            var lociToSearch = criteria.LociWithCriteriaSpecified().ToList();

            // TODO: NOVA-1395: Dynamically decide which loci to initially query for based on criteria, optimising for search speed
            // Need to consider the 2 mismatch case for a locus - the database search will assume at least one match, so is not suitable for two mismatch loci
            var lociToMatchInDatabase = new List<Locus> {Locus.B, Locus.Drb1}.Intersect(lociToSearch).ToList();
            var lociToMatchInMemory = new List<Locus> {Locus.A, Locus.C, Locus.Dqb1}.Intersect(lociToSearch);

            var matches = await databaseDonorMatchingService.FindMatchesForLoci(criteria, lociToMatchInDatabase);

            var matchesWithPGroupsPopulated = (await Task.WhenAll(matches.Select(PopulatePGroupsForMatch))).AsEnumerable();
            
            var matchesAtAllLoci = MatchInMemory(criteria, lociToMatchInMemory, matchesWithPGroupsPopulated);

            var filteredMatchesByMatchCriteria = matchesAtAllLoci
                .Where(m => matchFilteringService.FulfilsTotalMatchCriteria(m, criteria));
            
            var matchesWithDonorInfoPopulated = await Task.WhenAll(filteredMatchesByMatchCriteria.Select(PopulateDonorDataForMatch));

            var filteredMatchesByDonorInformation = matchesWithDonorInfoPopulated
                .Where(m => matchFilteringService.FulfilsRegistryCriteria(m, criteria))
                .Where(m => matchFilteringService.FulfilsSearchTypeCriteria(m, criteria))
                .Where(m => matchFilteringService.FulfilsSearchTypeSpecificCriteria(m, criteria))
                .ToList();
            
            // Once finished populating match data, mark data as populated (so that null locus match data can be accessed for mapping to the api model)
            filteredMatchesByDonorInformation.ForEach(m => m.MarkMatchingDataFullyPopulated());
            return filteredMatchesByDonorInformation;
        }

        /// <summary>
        /// Calculates match details for specified loci, and filters based on individual locus mismatch criteria
        /// </summary>
        /// <returns>A list of filtered search results, with the newly searched loci match information populated</returns>
        private IEnumerable<MatchResult> MatchInMemory(
            AlleleLevelMatchCriteria criteria,
            IEnumerable<Locus> lociToMatchInMemory,
            IEnumerable<MatchResult> matches)
        {
            foreach (var locus in lociToMatchInMemory)
            {
                var locusCriteria = criteria.MatchCriteriaForLocus(locus);
                foreach (var match in matches)
                {
                    var pGroupsAtLocus = match.DonorPGroups.DataAtLocus(locus);
                    var matchDetails = donorMatchCalculator.CalculateMatchDetailsForDonorHla(locusCriteria, pGroupsAtLocus);
                    match.SetMatchDetailsForLocus(locus, matchDetails);
                }

                matches = matches
                    .Where(m => matchFilteringService.FulfilsPerLocusMatchCriteria(m, criteria, locus))
                    .ToArray();
            }

            return matches;
        }

        private async Task<MatchResult> PopulatePGroupsForMatch(MatchResult matchResult)
        {
            var pGroups = await donorInspectionRepository.GetPGroupsForDonor(matchResult.DonorId);
            matchResult.DonorPGroups = pGroups;
            return matchResult;
        }

        private async Task<MatchResult> PopulateDonorDataForMatch(MatchResult matchResult)
        {
            // Augment each match with registry and other data from GetDonor(id)
            // Performance could be improved here, but at least it happens in parallel,
            // and only after filtering match results, not before.
            matchResult.Donor = await donorInspectionRepository.GetDonor(matchResult.DonorId);
            return matchResult;
        }
    }
}