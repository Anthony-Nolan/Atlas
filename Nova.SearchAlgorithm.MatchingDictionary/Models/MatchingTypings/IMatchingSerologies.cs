using System.Collections.Generic;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings
{
    public interface IMatchingSerologies
    {
        IEnumerable<SerologyTyping> MatchingSerologies { get; }
    }
}
