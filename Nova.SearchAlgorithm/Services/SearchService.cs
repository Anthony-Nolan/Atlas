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
            DonorMatchCriteria criteria = new DonorMatchCriteria
            {
                SearchType = searchRequest.SearchType,
                RegistriesToSearch = searchRequest.RegistriesToSearch,
                DonorMismatchCountTier1 = searchRequest.MatchCriteria.DonorMismatchCountTier1,
                DonorMismatchCountTier2 = searchRequest.MatchCriteria.DonorMismatchCountTier2,
                LocusMismatchA = MapMismatchToMatchCriteria("A", searchRequest.MatchCriteria.LocusMismatchA),
                LocusMismatchB = MapMismatchToMatchCriteria("B", searchRequest.MatchCriteria.LocusMismatchB),
                LocusMismatchC = MapMismatchToMatchCriteria("C", searchRequest.MatchCriteria.LocusMismatchC),
                LocusMismatchDRB1 = MapMismatchToMatchCriteria("DRB1", searchRequest.MatchCriteria.LocusMismatchDRB1),
                LocusMismatchDQB1 = MapMismatchToMatchCriteria("DQB1", searchRequest.MatchCriteria.LocusMismatchDQB1),
            };

            return donorRepository.Search(criteria);
        }

        private DonorLocusMatchCriteria MapMismatchToMatchCriteria(string locusName, LocusMismatchCriteria mismatch)
        {
            if (mismatch == null)
            {
                return null;
            }

            var hla1 = hlaRepository.RetrieveHlaMatches(locusName, mismatch.SearchHla1);
            var hla2 = hlaRepository.RetrieveHlaMatches(locusName, mismatch.SearchHla2);

            return new DonorLocusMatchCriteria
            {
                MismatchCount = mismatch.MismatchCount,
                HlaNamesToMatchInPositionOne = hla1.PGroups,
                HlaNamesToMatchInPositionTwo = hla2.PGroups,
            };
        }
    }
}