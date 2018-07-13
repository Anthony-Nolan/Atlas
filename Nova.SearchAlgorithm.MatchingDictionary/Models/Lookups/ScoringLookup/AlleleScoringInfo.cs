using System.Collections.Generic;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup
{
    public class AlleleScoringInfo : IHlaScoringInfo
    {
        public AlleleTypingStatus AlleleTypingStatus { get; }
        public string MatchingPGroup { get; }
        public string MatchingGGroup { get; }

        /// <summary>
        /// Matching serologies used when scoring the allele against a serology typing.
        /// </summary>
        public IEnumerable<SerologyEntry> MatchingSerologies { get; }

        public AlleleScoringInfo(
            AlleleTypingStatus alleleTypingStatus, 
            string matchingPGroup, 
            string matchingGGroup, 
            IEnumerable<SerologyEntry> matchingSerologies)
        {
            AlleleTypingStatus = alleleTypingStatus;
            MatchingPGroup = matchingPGroup;
            MatchingGGroup = matchingGGroup;
            MatchingSerologies = matchingSerologies;
        }
    }
}
