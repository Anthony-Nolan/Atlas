using System.Collections.Generic;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes
{
    public interface IMatchingPGroups : IMatchedHla
    {
        IEnumerable<string> MatchingPGroups { get; }
    }
}
