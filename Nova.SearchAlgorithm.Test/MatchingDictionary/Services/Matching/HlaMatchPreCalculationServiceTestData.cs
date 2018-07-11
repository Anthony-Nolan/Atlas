using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using Nova.SearchAlgorithm.Test.MatchingDictionary.Repositories.Wmda;
using System.Collections.Generic;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.Matching
{
    public sealed class HlaMatchPreCalculationServiceTestData
    {
        public static HlaMatchPreCalculationServiceTestData Instance { get; } = new HlaMatchPreCalculationServiceTestData();

        public IEnumerable<IMatchedHla> MatchedHla { get; }

        private HlaMatchPreCalculationServiceTestData()
        {
            MatchedHla = new HlaMatchPreCalculationService(WmdaRepositoryTestFixtureArgs.WmdaDataRepository).GetMatchedHla();
        }
    }
}
