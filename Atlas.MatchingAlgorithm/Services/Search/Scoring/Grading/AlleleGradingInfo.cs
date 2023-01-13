using Atlas.Common.GeneticData;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;

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