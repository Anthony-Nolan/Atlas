using System.Collections.Generic;

namespace Atlas.HlaMetadataDictionary.Models.MatchingTypings
{
    public interface IMatchingGGroups
    {
        IEnumerable<string> MatchingGGroups { get; }
    }
}
