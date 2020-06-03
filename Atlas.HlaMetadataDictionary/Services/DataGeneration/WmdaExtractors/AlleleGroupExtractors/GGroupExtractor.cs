using Atlas.HlaMetadataDictionary.WmdaDataAccess.Models;

namespace Atlas.HlaMetadataDictionary.Services.DataGeneration.WmdaExtractors.AlleleGroupExtractors
{
    internal class GGroupExtractor : AlleleGroupExtractorBase<HlaNomG>
    {
        private const string FileName = "hla_nom_g.txt";

        public GGroupExtractor() : base(FileName)
        {
        }
    }
}
