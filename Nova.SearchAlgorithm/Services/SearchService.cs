using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.Models;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Repositories.Donors;
using Nova.SearchAlgorithm.Repositories.Hla;
using Nova.SearchAlgorithm.Data.Repositories;
using Nova.SearchAlgorithm.Data.Models;

namespace Nova.SearchAlgorithm.Services
{
    public interface ISearchService
    {
        IEnumerable<PotentialMatch> Search(SearchRequest searchRequest);
    }

    public class SearchService : ISearchService
    {
        private readonly IDonorMatchRepository donorRepository;
        private readonly IHlaRepository hlaRepository;

        public SearchService(IDonorMatchRepository donorRepository, IHlaRepository hlaRepository)
        {
            this.donorRepository = donorRepository;
            this.hlaRepository = hlaRepository;
        }

        public IEnumerable<PotentialMatch> Search(SearchRequest searchRequest)
        {
            // TODO:NOVA-931 extend beyond locus A
            var hlaA1 = hlaRepository.RetrieveHlaMatches("A", searchRequest.MatchCriteria.LocusMismatchA.SearchHla1);
            var hlaA2 = hlaRepository.RetrieveHlaMatches("A", searchRequest.MatchCriteria.LocusMismatchA.SearchHla2);

            // TODO:NOVA-931 test antigen vs serology search behaviour
            DonorMatchCriteria criteria = new DonorMatchCriteria
            {
                SearchType = searchRequest.SearchType,
                RegistriesToSearch = searchRequest.RegistriesToSearch,
                DonorMismatchCountTier1 = searchRequest.MatchCriteria.DonorMismatchCountTier1,
                DonorMismatchCountTier2 = searchRequest.MatchCriteria.DonorMismatchCountTier2,
                LocusMismatchA = new DonorLocusMatchCriteria
                {
                    MismatchCount = searchRequest.MatchCriteria.LocusMismatchA.MismatchCount,
                    HlaNamesToMatchInPositionOne = searchRequest.MatchCriteria.LocusMismatchA.IsAntigenLevel ? hlaA1.PGroups : hlaA1.SerologyNames,
                    HlaNamesToMatchInPositionTwo = searchRequest.MatchCriteria.LocusMismatchA.IsAntigenLevel ? hlaA2.PGroups : hlaA2.SerologyNames,
                }
            };

            return donorRepository.Search(criteria);
        }
    }
}