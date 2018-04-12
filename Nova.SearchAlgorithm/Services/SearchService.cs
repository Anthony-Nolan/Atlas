using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.Models;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Repositories.Donors;
using Nova.SearchAlgorithm.Repositories.Hla;

namespace Nova.SearchAlgorithm.Services
{
    public interface ISearchService
    {
        IEnumerable<DonorMatch> Search(SearchRequest searchRequest);
    }

    public class SearchService : ISearchService
    {
        private readonly IDonorRepository donorRepository;
        private readonly IHlaRepository hlaRepository;

        public SearchService(IDonorRepository donorRepository, IHlaRepository hlaRepository)
        {
            this.donorRepository = donorRepository;
            this.hlaRepository = hlaRepository;
        }

        public IEnumerable<DonorMatch> Search(SearchRequest searchRequest)
        {
            // TODO:NOVA-931 extend beyond locus A
            MatchingHla hla1 = hlaRepository.RetrieveHlaMatches("A", searchRequest.MatchCriteria.LocusMismatchA.SearchHla1);
            MatchingHla hla2 = hlaRepository.RetrieveHlaMatches("A", searchRequest.MatchCriteria.LocusMismatchA.SearchHla2);

            // TODO:NOVA-931 test antigen vs serology search behaviour
            LocusSearchCriteria criteriaA = new LocusSearchCriteria
            {
                SearchType = searchRequest.SearchType,
                Registries = searchRequest.RegistriesToSearch,
                HlaNamesToMatchInPositionOne = searchRequest.MatchCriteria.LocusMismatchA.IsAntigenLevel ? hla1.MatchingProteinGroups : hla1.MatchingSerologyNames,
                HlaNamesToMatchInPositionTwo = searchRequest.MatchCriteria.LocusMismatchA.IsAntigenLevel ? hla2.MatchingProteinGroups : hla2.MatchingSerologyNames,
            };

            var matches = donorRepository.GetDonorMatchesAtLocus(searchRequest.SearchType, searchRequest.RegistriesToSearch, "A", criteriaA)
                .GroupBy(m => m.DonorId);

            if (searchRequest.MatchCriteria.LocusMismatchA.MismatchCount == 0)
            {
                matches = matches.Where(g => DirectMatch(g) || CrossMatch(g));
            }
            
            return matches.Select(DonorMatchFromGroup);
        }

        private bool DirectMatch(IEnumerable<HlaMatch> matches)
        {
            return matches.Where(m => m.SearchTypePosition == TypePositions.One && m.MatchingTypePositions.HasFlag(TypePositions.One)).Any()
                && matches.Where(m => m.SearchTypePosition == TypePositions.Two && m.MatchingTypePositions.HasFlag(TypePositions.Two)).Any();
        }

        private bool CrossMatch(IEnumerable<HlaMatch> matches)
        {
            return matches.Where(m => m.SearchTypePosition == TypePositions.One && m.MatchingTypePositions.HasFlag(TypePositions.Two)).Any()
                && matches.Where(m => m.SearchTypePosition == TypePositions.Two && m.MatchingTypePositions.HasFlag(TypePositions.One)).Any();
        }

        private DonorMatch DonorMatchFromGroup(IGrouping<int, HlaMatch> group)
        {
            var donor = donorRepository.GetDonor(group.Key).ToApiDonorMatch();

            // TODO:NOVA-931 extend beyond locus A
            donor.TotalMatchCount = DirectMatch(group) || CrossMatch(group) ? 2 : 1;

            return donor;
        }
    }
}