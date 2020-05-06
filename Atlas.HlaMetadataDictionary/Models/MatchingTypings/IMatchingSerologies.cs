using System.Collections.Generic;

namespace Atlas.MatchingAlgorithm.MatchingDictionary.Models.MatchingTypings
{
    public interface IMatchingSerologies
    {
        IEnumerable<MatchingSerology> MatchingSerologies { get; }
    }
}
