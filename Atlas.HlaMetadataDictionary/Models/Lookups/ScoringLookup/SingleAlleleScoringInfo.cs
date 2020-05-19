using Newtonsoft.Json;
using Atlas.HlaMetadataDictionary.Models.HLATypings;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Atlas.HlaMetadataDictionary.Models.Lookups.ScoringLookup
{
    /// <summary>
    /// Data needed to score a single allele typing.
    /// </summary>
    internal class SingleAlleleScoringInfo : 
        IHlaScoringInfo,
        IEquatable<SingleAlleleScoringInfo>
    {
        // Shortened property names are used when serialising the object for storage
        // to reduce the total row size

        [JsonProperty("name")]
        public string AlleleName { get; }

        [JsonProperty("status")]
        public AlleleTypingStatus AlleleTypingStatus { get; }

        [JsonProperty("pGrp")]
        public string MatchingPGroup { get; }

        [JsonProperty("gGrp")]
        public string MatchingGGroup { get; }

        [JsonProperty("ser")]
        public IEnumerable<SerologyEntry> MatchingSerologies { get; }

        [JsonIgnore]
        public IEnumerable<string> MatchingGGroups => new List<string> { MatchingGGroup };

        [JsonIgnore]
        public IEnumerable<string> MatchingPGroups => new List<string> { MatchingPGroup };

        public SingleAlleleScoringInfo(
            string alleleName,
            AlleleTypingStatus alleleTypingStatus,
            string matchingPGroup,
            string matchingGGroup,
            IEnumerable<SerologyEntry> matchingSerologies = null)
        {
            AlleleName = alleleName;
            AlleleTypingStatus = alleleTypingStatus;
            MatchingPGroup = matchingPGroup;
            MatchingGGroup = matchingGGroup;
            MatchingSerologies = matchingSerologies ?? new List<SerologyEntry>();
        }

        public static SingleAlleleScoringInfo GetScoringInfoWithMatchingSerologies(
            IHlaLookupResultSource<AlleleTyping> alleleSource)
        {
            return new SingleAlleleScoringInfo(
                alleleSource.TypingForHlaLookupResult.Name,
                alleleSource.TypingForHlaLookupResult.Status,
                alleleSource.MatchingPGroups.SingleOrDefault(),
                alleleSource.MatchingGGroups.SingleOrDefault(),
                alleleSource.MatchingSerologies.Select(m => m.ToSerologyEntry())
            );
        }

        public static SingleAlleleScoringInfo GetScoringInfoExcludingMatchingSerologies(
            IHlaLookupResultSource<AlleleTyping> alleleSource)
        {
            return new SingleAlleleScoringInfo(
                alleleSource.TypingForHlaLookupResult.Name,
                alleleSource.TypingForHlaLookupResult.Status,
                alleleSource.MatchingPGroups.SingleOrDefault(),
                alleleSource.MatchingGGroups.SingleOrDefault()
            );
        }

        public List<SingleAlleleScoringInfo> ConvertToSingleAllelesInfo() => new List<SingleAlleleScoringInfo>{this};

        #region IEquatable
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
                hashCode = (hashCode * 397) ^ (MatchingPGroup != null ? MatchingPGroup.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ MatchingGGroup.GetHashCode();
                hashCode = (hashCode * 397) ^ MatchingSerologies.GetHashCode();
                return hashCode;
            }
        }
        #endregion
    }
}
