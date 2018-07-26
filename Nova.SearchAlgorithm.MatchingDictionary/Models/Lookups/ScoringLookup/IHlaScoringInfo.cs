using System.Collections.Generic;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup
{
    public interface IHlaScoringInfo
    {
        /// <summary>
        /// Used when scoring against a serology typing.
        /// </summary>
        IEnumerable<SerologyEntry> MatchingSerologies { get; }
    }
}
