using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using System.Text.RegularExpressions;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.Wmda
{
    internal class SerologyToSerologyRelationshipExtractor : WmdaDataExtractor<RelSerSer>
    {
        private const string FileName = WmdaFilePathPrefix + "rel_ser_ser";
        private const string RegexPattern = @"(\w+)\;(\d*)\;([\d\/]*)\;([\d\/]*)";

        public SerologyToSerologyRelationshipExtractor() : base(FileName, RegexPattern, TypingMethod.Serology)
        {
        }

        protected override RelSerSer MapDataExtractedFromWmdaFile(GroupCollection extractedData)
        {
            return new RelSerSer(
                extractedData[1].Value,
                extractedData[2].Value,
                !extractedData[3].Value.Equals("") ? extractedData[3].Value.Split('/') : new string[] { },
                !extractedData[4].Value.Equals("") ? extractedData[4].Value.Split('/') : new string[] { }
            );
        }
    }
}
