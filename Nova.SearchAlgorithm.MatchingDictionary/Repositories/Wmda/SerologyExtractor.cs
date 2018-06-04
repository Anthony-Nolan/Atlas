using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.Wmda
{
    internal class SerologyExtractor : HlaNomExtractorBase
    {
        public SerologyExtractor() : base(TypingMethod.Serology)
        {
        }
    }
}
