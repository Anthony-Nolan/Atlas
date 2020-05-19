using System;
using Atlas.HlaMetadataDictionary.Models.HLATypings;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Atlas.HlaMetadataDictionary.Models.Lookups.ScoringLookup
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
            IHlaLookupResultSource<SerologyTyping> lookupResultSource)
        {
            return new SerologyScoringInfo(
                lookupResultSource.MatchingSerologies.Select(m => m.ToSerologyEntry()));
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
