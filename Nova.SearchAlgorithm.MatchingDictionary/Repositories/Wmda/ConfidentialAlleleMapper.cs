using System.Text.RegularExpressions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.Wmda
{
    internal class ConfidentialAlleleMapper : IWmdaDataMapper<ConfidentialAllele>
    {
        public ConfidentialAllele MapDataExtractedFromWmdaFile(GroupCollection matched)
        {
            return new ConfidentialAllele(
                matched[1].Value,
                matched[2].Value
            );
        }
    }
}
