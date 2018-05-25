using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using Nova.SearchAlgorithm.Test.MatchingDictionary.Data;
using System.Collections.Generic;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.Matching
{
    public sealed class HlaMatchingServiceTestData
    {
        public static HlaMatchingServiceTestData Instance { get; } = new HlaMatchingServiceTestData();

        public IEnumerable<IMatchedHla> MatchedHla { get; }

        private HlaMatchingServiceTestData()
        {
            var testFileImporter = new WmdaTestFileImporter();
            var repo = new WmdaDataRepository(testFileImporter);
            MatchedHla = new HlaMatchingService(repo).GetMatchedHla();
        }
    }
}
