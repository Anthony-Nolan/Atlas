using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using System.Text.RegularExpressions;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.Wmda
{
    internal class AlleleStatusExtractor : WmdaDataExtractor<AlleleStatus>
    {
        private const string FileName = "Allele_status.txt";
        private const string RegexPattern = @"^(\w+\*)([\w:]+),.+,(Full|Partial),(cDNA|gDNA)$";

        public AlleleStatusExtractor() : base(FileName)
        {
        }

        protected override AlleleStatus MapLineOfFileContentsToWmdaHlaTypingElseNull(string line)
        {
            var regex = new Regex(RegexPattern);

            if (!regex.IsMatch(line))
            {
                return null;
            }

            var extractedData = regex.Match(line).Groups;

            return new AlleleStatus(
                    extractedData[1].Value,
                    extractedData[2].Value,
                    extractedData[3].Value,
                    extractedData[4].Value
                );
        }
    }
}
