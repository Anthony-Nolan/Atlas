using Atlas.Common.GeneticData;
using Atlas.HlaMetadataDictionary.Models.HLATypings;
using Atlas.HlaMetadataDictionary.Models.Lookups.ScoringLookup;

namespace Atlas.MatchingAlgorithm.Services.Search.Scoring.Grading
{
    public class AlleleGradingInfo
    {
        public SingleAlleleScoringInfo ScoringInfo { get; }
        public AlleleTyping Allele { get; }

        public AlleleGradingInfo(Locus locus, SingleAlleleScoringInfo scoringInfo)
        {
            ScoringInfo = scoringInfo;
            Allele = scoringInfo.GenerateTypingAtLocus(locus);
        }
    }
}