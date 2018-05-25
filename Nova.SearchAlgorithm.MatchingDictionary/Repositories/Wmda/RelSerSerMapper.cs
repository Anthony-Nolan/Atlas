using System.Text.RegularExpressions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.Wmda
{
    internal class RelSerSerMapper : IWmdaDataMapper<RelSerSer>
    {
        public RelSerSer MapDataExtractedFromWmdaFile(GroupCollection matched)
        {
            return new RelSerSer(
                matched[1].Value,
                matched[2].Value,
                !matched[3].Value.Equals("") ? matched[3].Value.Split('/') : new string[] { },
                !matched[4].Value.Equals("") ? matched[4].Value.Split('/') : new string[] { }
            );
        }
    }
}
