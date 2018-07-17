using System.Collections.Generic;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup
{
    /// <summary>
    /// To be used with NMDP codes, allele strings & allele name variants that map to >1 allele,
    /// where info on each individual allele represented by the typing is needed for scoring.
    /// Not to be used with XX codes.
    /// </summary>
    public class MultipleAlleleScoringInfo : IHlaScoringInfo
    {
        public IEnumerable<SingleAlleleScoringInfo> AlleleScoringInfos { get; set; }

        public MultipleAlleleScoringInfo(IEnumerable<SingleAlleleScoringInfo> alleleScoringInfos)
        {
            AlleleScoringInfos = alleleScoringInfos;
        }
    }
}
