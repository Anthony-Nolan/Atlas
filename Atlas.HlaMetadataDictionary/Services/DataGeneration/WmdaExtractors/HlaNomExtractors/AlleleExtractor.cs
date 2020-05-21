using Atlas.Common.GeneticData.Hla.Models;

namespace Atlas.HlaMetadataDictionary.Services.DataGeneration.WmdaExtractors.HlaNomExtractors
{
    internal class AlleleExtractor : HlaNomExtractorBase
    {
        public AlleleExtractor() : base(TypingMethod.Molecular)
        {
        }
    }
}
