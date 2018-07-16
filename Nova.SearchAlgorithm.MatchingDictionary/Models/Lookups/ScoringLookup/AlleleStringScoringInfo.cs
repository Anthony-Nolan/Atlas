using System.Collections.Generic;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup
{
    /// <summary>
    /// To be used with NMDP codes & allele strings 
    /// where info on each individual allele is needed when scoring.
    /// Not to be used with XX codes.
    /// </summary>
    public class AlleleStringScoringInfo : IHlaScoringInfo
    {
        public IEnumerable<SingleAlleleScoringInfo> AlleleScoringInfos { get; set; }

        public AlleleStringScoringInfo(IEnumerable<SingleAlleleScoringInfo> alleleScoringInfos)
        {
            AlleleScoringInfos = alleleScoringInfos;
        }
    }
}
