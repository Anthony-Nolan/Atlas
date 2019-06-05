using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;

namespace Nova.SearchAlgorithm.Services.Scoring.Grading
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