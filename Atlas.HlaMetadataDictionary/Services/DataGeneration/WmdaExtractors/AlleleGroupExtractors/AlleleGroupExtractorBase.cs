using System.Text.RegularExpressions;
using Atlas.HlaMetadataDictionary.Models.Wmda;

namespace Atlas.HlaMetadataDictionary.Services.DataGeneration.WmdaExtractors.AlleleGroupExtractors
{
    internal abstract class AlleleGroupExtractorBase<TWmdaAlleleGroup> : WmdaDataExtractor<TWmdaAlleleGroup>
        where TWmdaAlleleGroup : IWmdaAlleleGroup, new()
    {
        private readonly Regex regex = new Regex(@"^(\w+\*)\;([\w:\/]+)\;([\w:]*)$");

        protected AlleleGroupExtractorBase(string fileName) : base(WmdaFilePathPrefix + fileName)
        {
        }

        protected override TWmdaAlleleGroup MapLineOfFileContentsToWmdaHlaTyping(string line)
        {
            if (!regex.IsMatch(line))
                return default;

            var extractedData = regex.Match(line).Groups;

            var alleleGroup = new TWmdaAlleleGroup
            {
                TypingLocus = extractedData[1].Value,
                Name = extractedData[3].Value.Equals("") ? extractedData[2].Value : extractedData[3].Value,
                Alleles = extractedData[2].Value.Split('/')
            };

            return alleleGroup;
        }
    }
}
