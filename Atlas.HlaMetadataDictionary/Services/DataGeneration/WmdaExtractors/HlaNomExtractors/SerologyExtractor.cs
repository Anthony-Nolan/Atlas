using Atlas.Common.GeneticData.Hla.Models;

namespace Atlas.HlaMetadataDictionary.Services.DataGeneration.WmdaExtractors.HlaNomExtractors
{
    internal class SerologyExtractor : HlaNomExtractorBase
    {
        public SerologyExtractor() : base(TypingMethod.Serology)
        {
        }
    }
}
