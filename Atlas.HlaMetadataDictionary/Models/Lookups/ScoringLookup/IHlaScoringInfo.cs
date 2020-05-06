using System.Collections.Generic;

namespace Atlas.MatchingAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup
{
    public interface IHlaScoringInfo
    {
        /// <summary>
        /// Used when scoring against a serology typing.
        /// </summary>
        IEnumerable<SerologyEntry> MatchingSerologies { get; }

        /// <summary>
        /// Used when scoring against a consolidated molecular typing.
        /// </summary>
        IEnumerable<string> MatchingGGroups { get; }

        /// <summary>
        /// Used when scoring against a consolidated molecular typing.
        /// </summary>
        IEnumerable<string> MatchingPGroups { get; }
    }
}
