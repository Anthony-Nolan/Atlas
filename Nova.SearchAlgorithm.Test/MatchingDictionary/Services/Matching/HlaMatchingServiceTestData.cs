using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda.Filters;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using Nova.SearchAlgorithm.Test.MatchingDictionary.Repositories;
using System.Collections.Generic;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.Matching
{
    public sealed class HlaMatchingServiceTestData
    {
        public static HlaMatchingServiceTestData Instance { get; } = new HlaMatchingServiceTestData();

        public IEnumerable<IMatchedHla> MatchedHla { get; }

        private HlaMatchingServiceTestData()
        {
            var repo = MockWmdaRepository.Instance;
            MatchedHla = new HlaMatchingService(repo).GetMatchedHla(SerologyFilter.Instance.Filter, MolecularFilter.Instance.Filter);
        }
    }
}
