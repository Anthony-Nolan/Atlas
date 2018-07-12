using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using Nova.SearchAlgorithm.Test.MatchingDictionary.Repositories.Wmda;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.HlaMatchPreCalculation
{
    // Some test data is shared between test fixtures and takes a long time to generate. 
    // This class will evaluate such data the first time it's requested, and serve it from a cache thereafter
    public static class SharedTestDataCache
    {
        private static List<IMatchedHla> _matchedHla;

        public static List<IMatchedHla> GetMatchedHla()
        {
            if (_matchedHla == null)
            {
                var hlaMatchPreCalculationService = new HlaMatchPreCalculationService(WmdaRepositoryTestFixtureArgs.WmdaDataRepository);
                _matchedHla = hlaMatchPreCalculationService.GetMatchedHla().ToList();
            }

            return _matchedHla;
        }
    }
}