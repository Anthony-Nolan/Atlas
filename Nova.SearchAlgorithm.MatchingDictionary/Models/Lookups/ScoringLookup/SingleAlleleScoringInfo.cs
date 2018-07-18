using System;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup
{
    /// <summary>
    /// Data needed to score a single allele typing.
    /// </summary>
    public class SingleAlleleScoringInfo : 
        IHlaScoringInfo,
        IEquatable<SingleAlleleScoringInfo>
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

        public static SingleAlleleScoringInfo GetScoringInfo(
            IHlaLookupResultSource<AlleleTyping> alleleSource)
        {
            return new SingleAlleleScoringInfo(
                alleleSource.TypingForHlaLookupResult.Name,
                alleleSource.TypingForHlaLookupResult.Status,
                alleleSource.MatchingPGroups.FirstOrDefault(),
                alleleSource.MatchingGGroups.FirstOrDefault(),
                alleleSource.MatchingSerologies.ToSerologyEntries()
            );
        }

        public bool Equals(SingleAlleleScoringInfo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return 
                string.Equals(AlleleName, other.AlleleName) &&
                AlleleTypingStatus.Equals(other.AlleleTypingStatus) &&
                string.Equals(MatchingPGroup, other.MatchingPGroup) &&
                string.Equals(MatchingGGroup, other.MatchingGGroup) &&
                MatchingSerologies.SequenceEqual(other.MatchingSerologies);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SingleAlleleScoringInfo) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = AlleleName.GetHashCode();
                hashCode = (hashCode * 397) ^ AlleleTypingStatus.GetHashCode();
                hashCode = (hashCode * 397) ^ MatchingPGroup.GetHashCode();
                hashCode = (hashCode * 397) ^ MatchingGGroup.GetHashCode();
                hashCode = (hashCode * 397) ^ MatchingSerologies.GetHashCode();
                return hashCode;
            }
        }
    }
}
