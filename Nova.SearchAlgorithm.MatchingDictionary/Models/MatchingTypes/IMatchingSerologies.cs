using System.Collections.Generic;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes
{
    public interface IMatchingSerologies
    {
        IEnumerable<Serology> MatchingSerologies { get; }
    }
}
