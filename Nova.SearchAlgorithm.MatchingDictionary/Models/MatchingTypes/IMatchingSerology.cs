using System.Collections.Generic;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes
{
    public interface IMatchingSerology
    {
        IEnumerable<Serology> MatchingSerologies { get; }
    }
}
