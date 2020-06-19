using Atlas.HlaMetadataDictionary.InternalModels.HLATypings;
using Atlas.HlaMetadataDictionary.InternalModels.MatchingTypings;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata
{
    public class SerologyScoringInfo : IHlaScoringInfo, IEquatable<SerologyScoringInfo>
    {
        public IEnumerable<SerologyEntry> MatchingSerologies { get; }
        public IEnumerable<string> MatchingGGroups { get; }
        public IEnumerable<string> MatchingPGroups { get; }

        [JsonConstructor]
        internal SerologyScoringInfo(
            IEnumerable<SerologyEntry> matchingSerologies,
            IEnumerable<string> matchingGGroups,
            IEnumerable<string> matchingPGroups)
        {
            MatchingSerologies = matchingSerologies;
            MatchingGGroups = matchingGGroups;
            MatchingPGroups = matchingPGroups;
        }

        internal static SerologyScoringInfo GetScoringInfo(
            IHlaMetadataSource<SerologyTyping> metadataSource)
        {
            var matchingSerologies = metadataSource.MatchingSerologies.Select(m => new SerologyEntry(m));
            return new SerologyScoringInfo(matchingSerologies, metadataSource.MatchingGGroups, metadataSource.MatchingPGroups);
        }

        public List<SingleAlleleScoringInfo> ConvertToSingleAllelesInfo()
            => throw new NotSupportedException($"Converting {nameof(SerologyScoringInfo)} to SingleAllele Info cannot be done quickly and is thus not currently supported.");

        #region Equality members

        public bool Equals(SerologyScoringInfo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return MatchingSerologies.SequenceEqual(other.MatchingSerologies) &&
                   MatchingGGroups.SequenceEqual(other.MatchingGGroups) &&
                   MatchingPGroups.SequenceEqual(other.MatchingPGroups);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SerologyScoringInfo)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(MatchingSerologies, MatchingGGroups, MatchingPGroups);
        }

        #endregion
    }
}
