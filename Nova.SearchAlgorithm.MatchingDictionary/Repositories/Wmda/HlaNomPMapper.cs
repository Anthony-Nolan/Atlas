using System.Text.RegularExpressions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.Wmda
{
    internal class HlaNomPMapper : IWmdaDataMapper<HlaNomP>
    {
        public HlaNomP MapDataExtractedFromWmdaFile(GroupCollection matched)
        {
            return new HlaNomP(
                matched[1].Value,
                matched[3].Value.Equals("") ? matched[2].Value : matched[3].Value,
                matched[2].Value.Split('/')
            );
        }
    }
}
