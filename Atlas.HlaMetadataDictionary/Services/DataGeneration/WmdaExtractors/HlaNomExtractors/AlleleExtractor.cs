using Atlas.HlaMetadataDictionary.Models.HLATypings;

namespace Atlas.HlaMetadataDictionary.Repositories.WmdaExtractors.HlaNomExtractors
{
    internal class AlleleExtractor : HlaNomExtractorBase
    {
        public AlleleExtractor() : base(TypingMethod.Molecular)
        {
        }
    }
}
