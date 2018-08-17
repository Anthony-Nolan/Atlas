using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using System.Text.RegularExpressions;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.Wmda
{
    internal class Dpb1TceGroupExtractor : WmdaDataExtractor<Dpb1TceGroup>
    {
        private const string FileName = "tce/dpb_tce.csv";
        private const string RegexPattern = @"^DPB1\*([\w:]+),DPB1\*([\w:]+),(\w?).*,(\w?).*,";

        public Dpb1TceGroupExtractor() : base(FileName)
        {
        }

        protected override Dpb1TceGroup MapLineOfFileContentsToWmdaHlaTypingElseNull(string line)
        {
            var regex = new Regex(RegexPattern);

            if (!regex.IsMatch(line))
            {
                return null;
            }

            var extractedData = regex.Match(line).Groups;

            return new Dpb1TceGroup(
                    extractedData[1].Value,
                    extractedData[2].Value,
                    extractedData[3].Value,
                    extractedData[4].Value
                );
        }
    }
}
