using Atlas.HlaMetadataDictionary.Models.HLATypings;

namespace Atlas.HlaMetadataDictionary.Repositories.WmdaExtractors.HlaNomExtractors
{
    internal class SerologyExtractor : HlaNomExtractorBase
    {
        public SerologyExtractor() : base(TypingMethod.Serology)
        {
        }
    }
}
