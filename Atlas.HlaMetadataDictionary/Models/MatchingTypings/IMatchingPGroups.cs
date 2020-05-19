using System.Collections.Generic;

namespace Atlas.HlaMetadataDictionary.Models.MatchingTypings
{
    internal interface IMatchingPGroups
    {
        IEnumerable<string> MatchingPGroups { get; }
    }
}
