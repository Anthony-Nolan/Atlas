using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using System.Text.RegularExpressions;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.Wmda
{
    internal class ConfidentialAlleleExtractor : WmdaDataExtractor<ConfidentialAllele>
    {
        private const string FileName = "version_report";
        private const string RegexPattern = @"^Confidential,(\w+\*)([\w:]+),";

        public ConfidentialAlleleExtractor() : base(FileName, RegexPattern, TypingMethod.Molecular)
        {
        }

        protected override ConfidentialAllele MapDataExtractedFromWmdaFile(GroupCollection extractedData)
        {
            return new ConfidentialAllele(
                extractedData[1].Value,
                extractedData[2].Value
            );
        }
    }
}
