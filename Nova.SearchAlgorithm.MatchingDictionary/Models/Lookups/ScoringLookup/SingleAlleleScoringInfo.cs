using System.Collections.Generic;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup
{
    /// <summary>
    /// Data needed to score a single allele typing.
    /// </summary>
    public class SingleAlleleScoringInfo : IHlaScoringInfo
    {
        public string AlleleName { get; }
        public AlleleTypingStatus AlleleTypingStatus { get; }
        public string MatchingPGroup { get; }
        public string MatchingGGroup { get; }

        /// <summary>
        /// Used when scoring against a serology typing.
        /// </summary>
        public IEnumerable<SerologyEntry> MatchingSerologies { get; }

        public SingleAlleleScoringInfo(
            string alleleName,
            AlleleTypingStatus alleleTypingStatus,
            string matchingPGroup,
            string matchingGGroup,
            IEnumerable<SerologyEntry> matchingSerologies)
        {
            AlleleName = alleleName;
            AlleleTypingStatus = alleleTypingStatus;
            MatchingPGroup = matchingPGroup;
            MatchingGGroup = matchingGGroup;
            MatchingSerologies = matchingSerologies;
        }
    }
}
