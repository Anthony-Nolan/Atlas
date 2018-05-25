using System.Text.RegularExpressions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.Wmda
{
    internal interface IWmdaDataMapper<out TWmdaHlaTyping> where TWmdaHlaTyping : IWmdaHlaTyping
    {
        TWmdaHlaTyping MapDataExtractedFromWmdaFile(GroupCollection matched);
    }
}
