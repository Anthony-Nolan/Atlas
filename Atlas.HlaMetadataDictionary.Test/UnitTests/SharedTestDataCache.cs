using System.Collections.Generic;
using System.Linq;
using Atlas.HlaMetadataDictionary.InternalModels.MatchingTypings;
using Atlas.HlaMetadataDictionary.Repositories;
using Atlas.HlaMetadataDictionary.Services.DataGeneration.HlaMatchPreCalculation;
using Atlas.HlaMetadataDictionary.Test.UnitTests.Data;

namespace Atlas.HlaMetadataDictionary.Test.UnitTests
{
    // Some test data is shared between test fixtures and takes a long time to generate. 
    // This class will evaluate such data the first time it's requested, and serve it from a cache thereafter
    internal static class SharedTestDataCache
    {
        // For the avoidance of doubt this "3330" is *NOT* the same as the "3330" in the FileBackedHlaMetadataDictionary.
        // This is "what is the path to the files that will be imported from disc rather than read from GitHub".
        // Nothing is particularly known to care what actual version of the HLA those files do or don't represent.
        public const string HlaNomenclatureVersionToTest = "3330";

        private static List<IMatchedHla> _matchedHla;
        private static WmdaDataRepository _wmdaDataRepository;

        public static IEnumerable<IMatchedHla> GetMatchedHla()
        {
            if (_matchedHla == null)
            {
                var wmdaDataRepository = GetWmdaDataRepository();
                var hlaMatchPreCalculationService = new HlaMatchPreCalculationService(wmdaDataRepository);
                _matchedHla = hlaMatchPreCalculationService.GetMatchedHla(HlaNomenclatureVersionToTest).ToList();
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