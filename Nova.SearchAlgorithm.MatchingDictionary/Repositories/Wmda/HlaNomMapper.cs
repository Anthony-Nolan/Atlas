using System.Text.RegularExpressions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.Wmda
{
    internal class HlaNomMapper : IWmdaDataMapper<HlaNom>
    {
        public HlaNom MapDataExtractedFromWmdaFile(GroupCollection matched)
        {
            return new HlaNom(
                matched[1].Value,
                matched[2].Value,
                !matched[3].Value.Equals(""),
                matched[4].Value);
        }
    }
}
