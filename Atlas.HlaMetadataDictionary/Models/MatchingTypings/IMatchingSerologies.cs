using System.Collections.Generic;

namespace Atlas.HlaMetadataDictionary.Models.MatchingTypings
{
    public interface IMatchingSerologies
    {
        IEnumerable<MatchingSerology> MatchingSerologies { get; }
    }
}
