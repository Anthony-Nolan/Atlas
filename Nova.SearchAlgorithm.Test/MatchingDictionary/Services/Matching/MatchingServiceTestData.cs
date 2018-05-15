using System.Collections.Generic;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda.Filters;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using Nova.SearchAlgorithm.MatchingDictionary.Services.Matching;
using Nova.SearchAlgorithm.Test.MatchingDictionary.Repositories;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.Matching
{
    public sealed class MatchingServiceTestData
    {
        public static MatchingServiceTestData Instance { get; } = new MatchingServiceTestData();

        public IEnumerable<IMatchedHla> AllMatchedHla { get; }
        public IEnumerable<IAlleleToPGroup> AllelesToPGroups { get; }
        public IEnumerable<ISerologyToSerology> SerologyToSerology { get; }

        private MatchingServiceTestData()
        {
            var repo = MockWmdaRepository.Instance;
            var alleleMatcher = new AlleleToPGroupMatching();
            var serologyMatcher = new SerologyToSerologyMatching();

            AllelesToPGroups = alleleMatcher.MatchAllelesToPGroups(repo, MolecularFilter.Instance.Filter);
            SerologyToSerology = serologyMatcher.MatchSerologyToSerology(repo, SerologyFilter.Instance.Filter);
            AllMatchedHla = new HlaMatchingService(repo)
                .MatchAllHla(SerologyFilter.Instance.Filter, MolecularFilter.Instance.Filter);
        }
    }
}
