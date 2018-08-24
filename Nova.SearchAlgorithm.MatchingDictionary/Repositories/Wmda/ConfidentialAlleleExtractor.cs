using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using System.Text.RegularExpressions;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.Wmda
{
    internal class ConfidentialAlleleExtractor : WmdaDataExtractor<ConfidentialAllele>
    {
        private const string FileName = "version_report.txt";
        private const string RegexPattern = @"^Confidential,(\w+\*)([\w:]+),";

        public ConfidentialAlleleExtractor() : base(FileName)
        {
        }

        protected override ConfidentialAllele MapLineOfFileContentsToWmdaHlaTypingElseNull(string line)
        {
            var regex = new Regex(RegexPattern);

            if (!regex.IsMatch(line))
                return null;

            var extractedData = regex.Match(line).Groups;

            return new ConfidentialAllele(
                    extractedData[1].Value,
                    extractedData[2].Value
                );
        }
    }
}
