using System.Collections.Generic;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using Nova.SearchAlgorithm.Test.MatchingDictionary.Repositories.Wmda;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.HlaMatchPreCalculation
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
