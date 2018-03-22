using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Repositories.SearchRequests;

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
            // TODO:NOVA-931 implement some basic matching logic
            hlaRepository.RetrieveHlaMatches();
            var matchingDonors = donorRepository.MatchDonors(searchRequest.MatchCriteria);
            return matchingDonors.Select(d => new DonorMatch
            {
                Donor = d,
                MatchDescription = MatchDescription.A & MatchDescription.B & MatchDescription.C & MatchDescription.DQB1 & MatchDescription.DRB1
            });
        }
    }
}