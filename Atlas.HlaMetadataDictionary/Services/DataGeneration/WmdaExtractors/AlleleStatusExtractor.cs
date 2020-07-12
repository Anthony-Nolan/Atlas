using System.Text.RegularExpressions;
using Atlas.HlaMetadataDictionary.WmdaDataAccess.Models;

namespace Atlas.HlaMetadataDictionary.Services.DataGeneration.WmdaExtractors
{
    internal class AlleleStatusExtractor : WmdaDataExtractor<AlleleStatus>
    {
        private const string FileName = "Allele_status.txt";
        private readonly Regex regex = new Regex(@"^(\w+\*)([\w:]+),.+,(Full|Partial),(cDNA|gDNA)$", RegexOptions.Compiled);

        public AlleleStatusExtractor() : base(FileName)
        {
        }

        protected override AlleleStatus MapLineOfFileContentsToWmdaHlaTyping(string line)
        {
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
