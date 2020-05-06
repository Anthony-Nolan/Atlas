using Atlas.MatchingAlgorithm.MatchingDictionary.Models.HLATypings;

namespace Atlas.MatchingAlgorithm.MatchingDictionary.Repositories.WmdaExtractors.HlaNomExtractors
{
    internal class SerologyExtractor : HlaNomExtractorBase
    {
        public SerologyExtractor() : base(TypingMethod.Serology)
        {
        }
    }
}
