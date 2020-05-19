using System.Collections.Generic;

namespace Atlas.HlaMetadataDictionary.Models.Lookups.ScoringLookup
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

        /// <summary>
        /// If it is possible to efficiently express the ScoringInfo Type as an array of <see cref="SingleAlleleScoringInfo"/>s then does so.
        /// Otherwise, throws <see cref="System.InvalidOperationException"/>
        /// </summary>
        public List<SingleAlleleScoringInfo> ConvertToSingleAllelesInfo();
    }
}
