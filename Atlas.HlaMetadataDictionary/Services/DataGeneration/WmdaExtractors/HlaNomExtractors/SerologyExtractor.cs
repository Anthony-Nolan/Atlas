using Atlas.HlaMetadataDictionary.Models.HLATypings;

namespace Atlas.HlaMetadataDictionary.Services.DataGeneration.WmdaExtractors.HlaNomExtractors
{
    internal class SerologyExtractor : HlaNomExtractorBase
    {
        public SerologyExtractor() : base(TypingMethod.Serology)
        {
        }
    }
}
