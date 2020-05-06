using System.Collections.Generic;
using System.Linq;
using Atlas.HlaMetadataDictionary.Models.MatchingTypings;
using Atlas.HlaMetadataDictionary.Repositories;
using Atlas.HlaMetadataDictionary.Services;
using Atlas.MatchingAlgorithm.Test.MatchingDictionary.Data;

namespace Atlas.MatchingAlgorithm.Test.MatchingDictionary
{
    // Some test data is shared between test fixtures and takes a long time to generate. 
    // This class will evaluate such data the first time it's requested, and serve it from a cache thereafter
    public static class SharedTestDataCache
    {
        public const string HlaDatabaseVersionToTest = "3330";

        private static List<IMatchedHla> _matchedHla;
        private static WmdaDataRepository _wmdaDataRepository;

        public static IEnumerable<IMatchedHla> GetMatchedHla()
        {
            if (_matchedHla == null)
            {
                var wmdaDataRepository = GetWmdaDataRepository();
                var hlaMatchPreCalculationService = new HlaMatchPreCalculationService(wmdaDataRepository);
                _matchedHla = hlaMatchPreCalculationService.GetMatchedHla(HlaDatabaseVersionToTest).ToList();
            }

            return _matchedHla;
        }


        public static WmdaDataRepository GetWmdaDataRepository()
        {
            if (_wmdaDataRepository == null)
            {
                var testFileReader = new WmdaTestFileImporter();
                _wmdaDataRepository = new WmdaDataRepository(testFileReader);
            }

            return _wmdaDataRepository;
        }
    }
}