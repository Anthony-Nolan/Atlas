using System.Text.RegularExpressions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.WmdaExtractors.AlleleGroupExtractors
{
    internal abstract class AlleleGroupExtractorBase<TWmdaAlleleGroup> : WmdaDataExtractor<TWmdaAlleleGroup>
        where TWmdaAlleleGroup : IWmdaAlleleGroup, new()
    {
        private readonly Regex regex = new Regex(@"^(\w+\*)\;([\w:\/]+)\;([\w:]*)$");

        protected AlleleGroupExtractorBase(string fileName) : base(WmdaFilePathPrefix + fileName)
        {
        }

        protected override TWmdaAlleleGroup MapLineOfFileContentsToWmdaHlaTypingElseNull(string line)
        {
            if (!regex.IsMatch(line))
                return default;

            var extractedData = regex.Match(line).Groups;

            var alleleGroup = new TWmdaAlleleGroup
            {
                Locus = extractedData[1].Value,
                Name = extractedData[3].Value.Equals("") ? extractedData[2].Value : extractedData[3].Value,
                Alleles = extractedData[2].Value.Split('/')
            };

            return alleleGroup;
        }
    }
}
