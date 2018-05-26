using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using System.Text.RegularExpressions;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.Wmda
{
    internal abstract class AlleleGroupExtractorBase<TWmdaAlleleGroup> : WmdaDataExtractor<TWmdaAlleleGroup>
        where TWmdaAlleleGroup : IWmdaAlleleGroup, new()
    {
        private const string RegexPattern = @"^(\w+\*)\;([\w:\/]+)\;([\w:]*)$";

        protected AlleleGroupExtractorBase(string fileName) : 
            base(WmdaFilePathPrefix + fileName, RegexPattern, TypingMethod.Molecular)
        {
        }

        protected override TWmdaAlleleGroup MapDataExtractedFromWmdaFile(GroupCollection extractedData)
        {
            var alleleGroup = new TWmdaAlleleGroup
            {
                WmdaLocus = extractedData[1].Value,
                Name = extractedData[3].Value.Equals("") ? extractedData[2].Value : extractedData[3].Value,
                Alleles = extractedData[2].Value.Split('/')
            };

            return alleleGroup;
        }
    }
}
