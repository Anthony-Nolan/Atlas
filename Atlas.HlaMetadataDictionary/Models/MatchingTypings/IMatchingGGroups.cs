using System.Collections.Generic;

namespace Atlas.HlaMetadataDictionary.Models.MatchingTypings
{
    internal interface IMatchingGGroups
    {
        IEnumerable<string> MatchingGGroups { get; }
    }
}
