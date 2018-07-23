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
        public IEnumerable<SingleAlleleScoringInfo> AlleleScoringInfos { get; set; }

        public MultipleAlleleScoringInfo(IEnumerable<SingleAlleleScoringInfo> alleleScoringInfos)
        {
            AlleleScoringInfos = alleleScoringInfos;
        }

        public static MultipleAlleleScoringInfo GetScoringInfo(
            IEnumerable<IHlaLookupResultSource<AlleleTyping>> lookupResultSources)
        {
            return new MultipleAlleleScoringInfo(
                lookupResultSources.Select(SingleAlleleScoringInfo.GetScoringInfo));
        }

        public bool Equals(MultipleAlleleScoringInfo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return AlleleScoringInfos.SequenceEqual(other.AlleleScoringInfos);
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
            return AlleleScoringInfos.GetHashCode();
        }
    }
}
