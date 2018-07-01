using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using Nova.SearchAlgorithm.Test.MatchingDictionary.Repositories.Wmda;
using System.Collections.Generic;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.Matching
{
    public sealed class HlaMatchingServiceTestData
    {
        public static HlaMatchingServiceTestData Instance { get; } = new HlaMatchingServiceTestData();

        public IEnumerable<IMatchedHla> MatchedHla { get; }

        private HlaMatchingServiceTestData()
        {
            MatchedHla = new HlaMatchingService(WmdaRepositoryTestFixtureArgs.WmdaDataRepository).GetMatchedHla();
        }
    }
}
