using System.Text.RegularExpressions;
using Atlas.HlaMetadataDictionary.WmdaDataAccess.Models;

namespace Atlas.HlaMetadataDictionary.Services.DataGeneration.WmdaExtractors
{
    internal class ConfidentialAlleleExtractor : WmdaDataExtractor<ConfidentialAllele>
    {
        private const string FileName = "version_report.txt";
        private readonly Regex regex = new Regex(@"^Confidential,(\w+\*)([\w:]+),");

        public ConfidentialAlleleExtractor() : base(FileName)
        {
        }

        protected override ConfidentialAllele MapLineOfFileContentsToWmdaHlaTyping(string line)
        {
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
