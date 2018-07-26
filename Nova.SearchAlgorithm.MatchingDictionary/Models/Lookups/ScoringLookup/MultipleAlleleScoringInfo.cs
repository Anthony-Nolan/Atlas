using System;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup
{
    /// <summary>
    /// To be used with NMDP codes, allele strings & allele name variants that map to >1 allele,
    /// where info on each individual allele represented by the typing is needed for scoring.
    /// Not to be used with XX codes.
    /// </summary>
    public class MultipleAlleleScoringInfo : 
        IHlaScoringInfo, 
        IEquatable<MultipleAlleleScoringInfo>
    {
        public IEnumerable<SingleAlleleScoringInfo> AlleleScoringInfos { get; }
        public IEnumerable<SerologyEntry> MatchingSerologies { get; }

        public MultipleAlleleScoringInfo(
            IEnumerable<SingleAlleleScoringInfo> alleleScoringInfos, 
            IEnumerable<SerologyEntry> matchingSerologies)
        {
            AlleleScoringInfos = alleleScoringInfos;
            MatchingSerologies = matchingSerologies;
        }

        public static MultipleAlleleScoringInfo GetScoringInfo(
            IEnumerable<IHlaLookupResultSource<AlleleTyping>> lookupResultSources)
        {
            var sources = lookupResultSources.ToList();

            var alleleScoringInfos = sources
                .Select(SingleAlleleScoringInfo.GetScoringInfoExcludingMatchingSerologies);

            var matchingSerologies = sources
                .SelectMany(source => source.MatchingSerologies)
                .Select(matchingSerology => matchingSerology.ToSerologyEntry())
                .Distinct();

            return new MultipleAlleleScoringInfo(
                alleleScoringInfos,
                matchingSerologies);
        }

        public bool Equals(MultipleAlleleScoringInfo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return 
                AlleleScoringInfos.SequenceEqual(other.AlleleScoringInfos) && 
                MatchingSerologies.SequenceEqual(other.MatchingSerologies);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MultipleAlleleScoringInfo) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (AlleleScoringInfos.GetHashCode() * 397) ^ MatchingSerologies.GetHashCode();
            }
        }
    }
}
