using System;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup
{
    /// <summary>
    /// Only to be used with XX codes where consolidated data is sufficient for scoring.
    /// </summary>
    public class XxCodeScoringInfo : 
        IHlaScoringInfo,
        IEquatable<XxCodeScoringInfo>
    {
        public IEnumerable<string> MatchingPGroups { get; }
        public IEnumerable<string> MatchingGGroups { get; }
        public IEnumerable<SerologyEntry> MatchingSerologies { get; }

        public XxCodeScoringInfo(
            IEnumerable<string> matchingPGroups,
            IEnumerable<string> matchingGGroups, 
            IEnumerable<SerologyEntry> matchingSerologies)
        {
            MatchingPGroups = matchingPGroups;
            MatchingGGroups = matchingGGroups;
            MatchingSerologies = matchingSerologies;
        }

        public static XxCodeScoringInfo GetScoringInfo(
            IEnumerable<IHlaLookupResultSource<AlleleTyping>> allelesSource)
        {
            var alleles = allelesSource.ToList();

            return new XxCodeScoringInfo(
                alleles.SelectMany(allele => allele.MatchingPGroups).Distinct(),
                alleles.SelectMany(allele => allele.MatchingGGroups).Distinct(),
                alleles.SelectMany(allele => allele.MatchingSerologies.Select(m => m.ToSerologyEntry())).Distinct()
                );
        }

        public bool Equals(XxCodeScoringInfo other)
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
            return Equals((XxCodeScoringInfo) obj);
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
    }
}
