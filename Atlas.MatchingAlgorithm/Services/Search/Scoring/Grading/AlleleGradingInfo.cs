using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.MatchingDictionary.Models.HLATypings;
using Atlas.MatchingAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;

namespace Atlas.MatchingAlgorithm.Services.Scoring.Grading
{
    public class AlleleGradingInfo
    {
        public SingleAlleleScoringInfo ScoringInfo { get; }
        public AlleleTyping Allele { get; }

        public AlleleGradingInfo(Locus locus, IHlaScoringInfo scoringInfo)
        {
            ScoringInfo = (SingleAlleleScoringInfo)scoringInfo;
            Allele = new AlleleTyping(locus, ScoringInfo.AlleleName);
        }
    }
}