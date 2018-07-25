using System.Collections.Generic;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings
{
    public interface IMatchingSerologies
    {
        IEnumerable<MatchingSerology> MatchingSerologies { get; }
    }
}
