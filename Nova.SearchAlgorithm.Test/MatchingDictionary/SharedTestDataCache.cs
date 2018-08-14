using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using Nova.SearchAlgorithm.Test.MatchingDictionary.Data;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary
{
    // Some test data is shared between test fixtures and takes a long time to generate. 
    // This class will evaluate such data the first time it's requested, and serve it from a cache thereafter
    public static class SharedTestDataCache
    {
        private const string HlaDatabaseVersionToTest = "3330";
        
        private static List<IMatchedHla> _matchedHla;
        private static WmdaDataRepository _wmdaDataRepository;

        public static List<IMatchedHla> GetMatchedHla()
        {
            if (_matchedHla == null)
            {
                var wmdaDataRepository = GetWmdaDataRepository();
                var hlaMatchPreCalculationService = new HlaMatchPreCalculationService(wmdaDataRepository);
                _matchedHla = hlaMatchPreCalculationService.GetMatchedHla().ToList();
            }

            return _matchedHla;
        }


        public static WmdaDataRepository GetWmdaDataRepository()
        {
            if (_wmdaDataRepository == null)
            {
                var testFileReader = new WmdaTestFileImporter();
                _wmdaDataRepository = new WmdaDataRepository(testFileReader, HlaDatabaseVersionToTest);
            }

            return _wmdaDataRepository;
        }
    }
}