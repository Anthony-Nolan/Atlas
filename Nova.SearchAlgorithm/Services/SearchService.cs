using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    static class MatchCriteriaExtensions
    {
        public static FiveLociDetails<LocusMismatchCriteria> LocusCriteria(this MismatchCriteria matchCriteria)
        {
            return new FiveLociDetails<LocusMismatchCriteria>
            {
                A = matchCriteria.LocusMismatchA,
                B = matchCriteria.LocusMismatchB,
                C = matchCriteria.LocusMismatchC,
                DQB1 = matchCriteria.LocusMismatchDQB1,
                DRB1 = matchCriteria.LocusMismatchDRB1
            };
        }

        public static SingleLocusDetails<string> SearchHla(this LocusMismatchCriteria locusCriteria)
        {
            return new SingleLocusDetails<string>
            {
                One = locusCriteria.SearchHla1,
                Two = locusCriteria.SearchHla2
            };
        }
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

            var searchCriteria = new SearchCriteria
            {
                LocusMatchCriteria = searchRequest.MatchCriteria.LocusCriteria().Map((string a, LocusMismatchCriteria b) => hlaRepository.RetrieveHlaMatches(a, b.SearchHla())),
                SearchType = searchRequest.SearchType,
                Registries = searchRequest.RegistriesToSearch
            };

            var matchingDonors = donorRepository.MatchDonors(searchCriteria);
            return matchingDonors.Select(d => d.ToApiDonorMatch());
        }
    }
}