using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using System.Text.RegularExpressions;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.Wmda
{
    internal abstract class HlaNomExtractorBase : WmdaDataExtractor<HlaNom>
    {
        private const string FileName = WmdaFilePathPrefix + "hla_nom";
        private const string RegexPattern = @"^(\w+\*{0,1})\;([\w:]+)\;\d+\;(\d*)\;([\w:]*)\;";

        protected HlaNomExtractorBase(TypingMethod typingMethod) : base(FileName, RegexPattern, typingMethod)
        {
        }

        protected override HlaNom MapDataExtractedFromWmdaFile(GroupCollection extractedData)
        {
            return new HlaNom(
                extractedData[1].Value,
                extractedData[2].Value,
                !extractedData[3].Value.Equals(""),
                extractedData[4].Value);
        }
    }
}
