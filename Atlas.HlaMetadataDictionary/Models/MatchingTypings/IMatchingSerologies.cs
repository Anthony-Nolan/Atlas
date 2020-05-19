using System.Collections.Generic;

namespace Atlas.HlaMetadataDictionary.Models.MatchingTypings
{
    internal interface IMatchingSerologies
    {
        IEnumerable<MatchingSerology> MatchingSerologies { get; }
    }
}
