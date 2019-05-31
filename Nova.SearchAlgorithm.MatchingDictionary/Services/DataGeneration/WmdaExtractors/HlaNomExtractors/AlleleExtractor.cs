using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.WmdaExtractors.HlaNomExtractors
{
    internal class AlleleExtractor : HlaNomExtractorBase
    {
        public AlleleExtractor() : base(TypingMethod.Molecular)
        {
        }
    }
}
