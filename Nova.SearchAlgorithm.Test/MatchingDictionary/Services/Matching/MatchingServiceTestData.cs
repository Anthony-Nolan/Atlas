using System.Collections.Generic;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda.Filters;
using Nova.SearchAlgorithm.MatchingDictionary.Services.Matching;
using Nova.SearchAlgorithm.Test.MatchingDictionary.Repositories;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.Matching
{
    public sealed class MatchingServiceTestData
    {
        public static MatchingServiceTestData Instance { get; } = new MatchingServiceTestData();

        public IEnumerable<IMatchedHla> AllMatchedHla { get; }
        public IEnumerable<IAlleleToPGroup> AllelesToPGroups { get; }
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
