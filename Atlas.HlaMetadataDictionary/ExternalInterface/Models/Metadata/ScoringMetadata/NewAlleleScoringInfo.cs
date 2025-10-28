using System;
using System.Collections.Generic;
using System.Linq;

namespace Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata
{
    public class NewAlleleScoringInfo : IHlaScoringInfo, IEquatable<NewAlleleScoringInfo>
    {
        public IEnumerable<SerologyEntry> MatchingSerologies { get; } = new List<SerologyEntry>();
        public IEnumerable<string> MatchingGGroups { get; } = new List<string>();
        public IEnumerable<string> MatchingPGroups { get; } = new List<string>();

        public NewAlleleScoringInfo()
        {
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as NewAlleleScoringInfo);
        }

        public bool Equals(NewAlleleScoringInfo other)
        {
            if (other == null) return false;

            return MatchingGGroups.SequenceEqual(other.MatchingGGroups)
                   && MatchingPGroups.SequenceEqual(other.MatchingPGroups)
                   && MatchingSerologies.SequenceEqual(other.MatchingSerologies);
        }

        public override int GetHashCode()
        {
            // Combine hashes of collections
            int hash = 17;
            foreach (var g in MatchingGGroups) hash = hash * 31 + g.GetHashCode();
            foreach (var p in MatchingPGroups) hash = hash * 31 + p.GetHashCode();
            foreach (var s in MatchingSerologies) hash = hash * 31 + s.GetHashCode();
            return hash;
        }

        List<SingleAlleleScoringInfo> IHlaScoringInfo.ConvertToSingleAllelesInfo()
        {
            throw new NotImplementedException();
        }
    }
}
