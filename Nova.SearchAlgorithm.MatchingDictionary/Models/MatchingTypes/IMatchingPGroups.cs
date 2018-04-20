using System.Collections.Generic;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes
{
    public interface IMatchingPGroups : IMatchedOn
    {
        IEnumerable<string> MatchingPGroups { get; }
    }
}
