﻿using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings;
using Atlas.HlaMetadataDictionary.InternalModels.MatchingTypings;
using Newtonsoft.Json;

namespace Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata
{
    /// <summary>
    /// Only to be used with molecular types, such as NMDP codes & XX codes,
    /// where consolidated data is sufficient for scoring.
    /// </summary>
    public class ConsolidatedMolecularScoringInfo : 
        IHlaScoringInfo,
        IEquatable<ConsolidatedMolecularScoringInfo>
    {
        public IEnumerable<string> MatchingPGroups { get; }
        public IEnumerable<string> MatchingGGroups { get; }
        public IEnumerable<SerologyEntry> MatchingSerologies { get; }

        [JsonConstructor]
        internal ConsolidatedMolecularScoringInfo(
            IEnumerable<string> matchingPGroups,
            IEnumerable<string> matchingGGroups, 
            IEnumerable<SerologyEntry> matchingSerologies)
        {
            MatchingPGroups = matchingPGroups;
            MatchingGGroups = matchingGGroups;
            MatchingSerologies = matchingSerologies;
        }

        internal static ConsolidatedMolecularScoringInfo GetScoringInfo(
            IEnumerable<IHlaMetadataSource<AlleleTyping>> allelesSource)
        {
            var alleles = allelesSource.ToList();

            return new ConsolidatedMolecularScoringInfo(
                alleles.SelectMany(allele => allele.MatchingPGroups).Distinct(),
                alleles.SelectMany(allele => allele.MatchingGGroups).Distinct(),
                alleles.SelectMany(allele => allele.MatchingSerologies.Select(m => new SerologyEntry(m))).Distinct()
                );
        }

        public List<SingleAlleleScoringInfo> ConvertToSingleAllelesInfo()
            => throw new NotSupportedException($"Converting {nameof(ConsolidatedMolecularScoringInfo)} to SingleAllele Info cannot be done quickly and is thus not currently supported.");

        #region IEquatable
        public bool Equals(ConsolidatedMolecularScoringInfo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return 
                MatchingPGroups.SequenceEqual(other.MatchingPGroups) && 
                MatchingGGroups.SequenceEqual(other.MatchingGGroups) && 
                MatchingSerologies.SequenceEqual(other.MatchingSerologies);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ConsolidatedMolecularScoringInfo) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = MatchingPGroups.GetHashCode();
                hashCode = (hashCode * 397) ^ MatchingGGroups.GetHashCode();
                hashCode = (hashCode * 397) ^ MatchingSerologies.GetHashCode();
                return hashCode;
            }
        }
        #endregion
    }
}
