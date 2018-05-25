using System.Text.RegularExpressions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.Wmda
{
    internal class HlaNomGMapper : IWmdaDataMapper<HlaNomG>
    {
        public HlaNomG MapDataExtractedFromWmdaFile(GroupCollection matched)
        {
            return new HlaNomG(
                matched[1].Value,
                matched[3].Value.Equals("") ? matched[2].Value : matched[3].Value,
                matched[2].Value.Split('/')
            );
        }
    }
}
