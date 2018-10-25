using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Models.SearchResults;
using Nova.SearchAlgorithm.Common.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Services.Matching
{
    public interface IDonorMatchingService
    {
        Task<IEnumerable<MatchResult>> GetMatches(AlleleLevelMatchCriteria criteria);
    }

    public class DonorMatchingService : IDonorMatchingService
    {
        private readonly IDatabaseDonorMatchingService databaseDonorMatchingService;
        private readonly IDonorInspectionRepository donorInspectionRepository;
        private readonly IMatchFilteringService matchFilteringService;
        private readonly IMatchCriteriaAnalyser matchCriteriaAnalyser;

        public DonorMatchingService(
            IDatabaseDonorMatchingService databaseDonorMatchingService,
            IDonorInspectionRepository donorInspectionRepository,
            IMatchFilteringService matchFilteringService,
            IMatchCriteriaAnalyser matchCriteriaAnalyser)
        {
            this.databaseDonorMatchingService = databaseDonorMatchingService;
            this.donorInspectionRepository = donorInspectionRepository;
            this.matchFilteringService = matchFilteringService;
            this.matchCriteriaAnalyser = matchCriteriaAnalyser;
        }

        public async Task<IEnumerable<MatchResult>> GetMatches(AlleleLevelMatchCriteria criteria)
        {
            var lociToMatchFirst = matchCriteriaAnalyser.LociToMatchFirst(criteria).ToList();
            var lociToMatchSecond = criteria.LociWithCriteriaSpecified().Except(lociToMatchFirst).ToList();

            var initialMatches = (await databaseDonorMatchingService.FindMatchesForLoci(criteria, lociToMatchFirst)).ToList();
            var matchesAtAllLoci =
                (await databaseDonorMatchingService.FindMatchesForLociFromDonorSelection(criteria, lociToMatchSecond, initialMatches))
                .ToList();

            var filteredMatchesByMatchCriteria = matchesAtAllLoci.Where(m => matchFilteringService.FulfilsTotalMatchCriteria(m, criteria));

            var matchesWithDonorInfoPopulated = await PopulateDonorData(filteredMatchesByMatchCriteria);

            var filteredMatchesByDonorInformation = matchesWithDonorInfoPopulated
                .Where(m => matchFilteringService.FulfilsRegistryCriteria(m, criteria))
                .Where(m => matchFilteringService.FulfilsSearchTypeCriteria(m, criteria))
                .Where(m => matchFilteringService.FulfilsSearchTypeSpecificCriteria(m, criteria))
                .ToList();

            // Once finished populating match data, mark data as populated (so that null locus match data can be accessed for mapping to the api model)
            filteredMatchesByDonorInformation.ForEach(m => m.MarkMatchingDataFullyPopulated());
            return filteredMatchesByDonorInformation;
        }

        private async Task<IEnumerable<MatchResult>> PopulateDonorData(IEnumerable<MatchResult> filteredMatchesByMatchCriteria)
        {
            filteredMatchesByMatchCriteria = filteredMatchesByMatchCriteria.ToList();
            var donorIds = filteredMatchesByMatchCriteria.Select(m => m.DonorId);
            var donors = (await donorInspectionRepository.GetDonors(donorIds)).ToList();
            foreach (var match in filteredMatchesByMatchCriteria)
            {
                match.Donor = donors.Single(d => d.DonorId == match.DonorId);
            }

            return filteredMatchesByMatchCriteria;
        }
    }
}