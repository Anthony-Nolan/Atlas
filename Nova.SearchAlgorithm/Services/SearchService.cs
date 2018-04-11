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
        public static PhenotypeInfo<string> LocusCriteria(this MismatchCriteria matchCriteria)
        {
            return new PhenotypeInfo<string>
            {
                A_1 = matchCriteria.LocusMismatchA?.SearchHla1,
                A_2 = matchCriteria.LocusMismatchA?.SearchHla2,
                B_1 = matchCriteria.LocusMismatchB?.SearchHla1,
                B_2 = matchCriteria.LocusMismatchB?.SearchHla2,
                C_1 = matchCriteria.LocusMismatchC?.SearchHla1,
                C_2 = matchCriteria.LocusMismatchC?.SearchHla2,
                DQB1_1 = matchCriteria.LocusMismatchDQB1?.SearchHla1,
                DQB1_2 = matchCriteria.LocusMismatchDQB1?.SearchHla2,
                DRB1_1 = matchCriteria.LocusMismatchDRB1?.SearchHla1,
                DRB1_2 = matchCriteria.LocusMismatchDRB1?.SearchHla2
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
                LocusMatchCriteria = searchRequest.MatchCriteria.LocusCriteria().Map((string locus, int position, string name) => hlaRepository.RetrieveHlaMatches(locus, name)),
                SearchType = searchRequest.SearchType,
                Registries = searchRequest.RegistriesToSearch
            };

            var matchingDonors = donorRepository.MatchDonors(searchCriteria);
            return matchingDonors.Select(d => d.ToApiDonorMatch());
        }
    }
}