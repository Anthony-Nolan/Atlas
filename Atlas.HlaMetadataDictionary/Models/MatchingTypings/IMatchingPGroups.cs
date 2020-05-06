using System.Collections.Generic;

namespace Atlas.HlaMetadataDictionary.Models.MatchingTypings
{
    public interface IMatchingPGroups
    {
        IEnumerable<string> MatchingPGroups { get; }
    }
}
