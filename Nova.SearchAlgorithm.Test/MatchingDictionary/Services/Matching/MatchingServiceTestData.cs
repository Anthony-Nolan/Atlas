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
        public IEnumerable<IAlleleInfoForMatching> AllelesToPGroups { get; }
        public IEnumerable<ISerologyInfoForMatching> SerologyToSerology { get; }

        private MatchingServiceTestData()
        {
            var repo = MockWmdaRepository.Instance;
            var alleleMatcher = new AlleleInfoGenerator();
            var serologyMatcher = new SerologyInfoGenerator();

            AllelesToPGroups = alleleMatcher.GetAlleleInfoForMatching(repo, MolecularFilter.Instance.Filter);
            SerologyToSerology = serologyMatcher.GetSerologyInfoForMatching(repo, SerologyFilter.Instance.Filter);
            AllMatchedHla = new HlaMatchingService(repo)
                .GetMatchedHla(SerologyFilter.Instance.Filter, MolecularFilter.Instance.Filter);
        }
    }
}
