using Atlas.MatchingAlgorithm.MatchingDictionary.Models.HLATypings;

namespace Atlas.MatchingAlgorithm.MatchingDictionary.Repositories.WmdaExtractors.HlaNomExtractors
{
    internal class AlleleExtractor : HlaNomExtractorBase
    {
        public AlleleExtractor() : base(TypingMethod.Molecular)
        {
        }
    }
}
