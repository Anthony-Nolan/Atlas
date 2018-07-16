using System.Collections.Generic;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup
{
    /// <summary>
    /// Only to be used with XX codes where consolidated data is sufficient for scoring.
    /// </summary>
    public class XxCodeScoringInfo : IHlaScoringInfo
    {
        public IEnumerable<string> MatchingPGroups { get; }
        public IEnumerable<string> MatchingGGroups { get; }

        /// <summary>
        /// Used when scoring against a serology typing.
        /// </summary>
        public IEnumerable<SerologyEntry> MatchingSerologies { get; }

        public XxCodeScoringInfo(
            IEnumerable<string> matchingPGroups,
            IEnumerable<string> matchingGGroups, 
            IEnumerable<SerologyEntry> matchingSerologies)
        {
            MatchingPGroups = matchingPGroups;
            MatchingGGroups = matchingGGroups;
            MatchingSerologies = matchingSerologies;
        }
    }
}
