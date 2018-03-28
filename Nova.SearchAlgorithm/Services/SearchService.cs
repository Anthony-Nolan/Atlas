using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Models;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Repositories.Donors;
using Nova.SearchAlgorithm.Repositories.Hlas;


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
            var hlaAMatches = hlaRepository.RetrieveHlaMatches(
                "A",
                searchRequest.MatchCriteria.LocusMismatchA.SearchHla1,
                searchRequest.MatchCriteria.LocusMismatchA.SearchHla2);

            var searchCriteria = new SearchCriteria
            {
                LocusA = hlaAMatches,
                SearchType = searchRequest.SearchType,
                Registries = searchRequest.RegistriesToSearch
            };

            var matchingDonors = donorRepository.MatchDonors(searchCriteria);
            return matchingDonors.Select(d => new DonorMatch
            {
                Donor = d.ToApiDonor(),
                MatchDescription = MatchDescription.A & MatchDescription.B & MatchDescription.C & MatchDescription.DQB1 & MatchDescription.DRB1
            });
        }
    }
}