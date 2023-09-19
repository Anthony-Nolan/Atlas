using System.Collections.Generic;

namespace Atlas.HlaMetadataDictionary.InternalModels.MatchingTypings
{
    internal interface IMatchedHla : IMatchedOn
    {
        public List<string> MatchingPGroups { get; }
        public List<string> MatchingGGroups { get; }
        public IEnumerable<MatchingSerology> MatchingSerologies { get; }
    }
}