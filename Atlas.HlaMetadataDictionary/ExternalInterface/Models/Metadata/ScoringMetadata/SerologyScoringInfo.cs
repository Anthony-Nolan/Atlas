using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.HlaMetadataDictionary.InternalModels.HLATypings;
using Atlas.HlaMetadataDictionary.InternalModels.MatchingTypings;
using Newtonsoft.Json;

namespace Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata
{
    public class SerologyScoringInfo : 
        IHlaScoringInfo,
        IEquatable<SerologyScoringInfo>
    {
        public IEnumerable<SerologyEntry> MatchingSerologies { get; }

        [JsonIgnore]
        public IEnumerable<string> MatchingGGroups => new List<string>();

        [JsonIgnore]
        public IEnumerable<string> MatchingPGroups => new List<string>();

        [JsonConstructor]
        internal SerologyScoringInfo(
            IEnumerable<SerologyEntry> matchingSerologies)
        {
            MatchingSerologies = matchingSerologies;
        }

        internal static SerologyScoringInfo GetScoringInfo(
            IHlaMetadataSource<SerologyTyping> metadataSource)
        {
            return new SerologyScoringInfo(
                metadataSource.MatchingSerologies.Select(m => new SerologyEntry(m)));
        }

        public List<SingleAlleleScoringInfo> ConvertToSingleAllelesInfo()
            => throw new NotSupportedException($"Converting {nameof(SerologyScoringInfo)} to SingleAllele Info cannot be done quickly and is thus not currently supported.");

        #region IEquatable
        public bool Equals(SerologyScoringInfo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return MatchingSerologies.SequenceEqual(other.MatchingSerologies);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SerologyScoringInfo) obj);
        }

        public override int GetHashCode()
        {
            return MatchingSerologies.GetHashCode();
        }
        #endregion
    }
}
