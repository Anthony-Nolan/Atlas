using System.Collections.Generic;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda.Filters;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using Nova.SearchAlgorithm.MatchingDictionary.Tests.Repositories;

namespace Nova.SearchAlgorithm.MatchingDictionary.Tests.Services
{
    public sealed class MatchingServiceTestData
    {
        public static MatchingServiceTestData Instance { get; } = new MatchingServiceTestData();

        public IEnumerable<IMatchedHla> AllMatchedHla { get; }
        public IEnumerable<IMatchingPGroups> AllelesToPGroups { get; }
        public IEnumerable<IMatchingSerology> SerologyToSerology { get; }

        private MatchingServiceTestData()
        {
            var repo = MockWmdaRepository.Instance;
            var alleleMatcher = new AlleleMatchingService(repo);
            var serologyMatcher = new SerologyMatchingService(repo);

            AllelesToPGroups = alleleMatcher.MatchAllelesToPGroups(MolecularFilter.Instance.Filter);
            SerologyToSerology = serologyMatcher.MatchSerologyToSerology(SerologyFilter.Instance.Filter);
            AllMatchedHla = new HlaMatchingService(repo, alleleMatcher, serologyMatcher)
                .MatchAllHla(SerologyFilter.Instance.Filter, MolecularFilter.Instance.Filter);
        }
    }
}
