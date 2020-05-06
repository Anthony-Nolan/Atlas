using Atlas.HlaMetadataDictionary.Models.Wmda;

namespace Atlas.HlaMetadataDictionary.Repositories.WmdaExtractors.AlleleGroupExtractors
{
    internal class PGroupExtractor : AlleleGroupExtractorBase<HlaNomP>
    {
        private const string FileName = "hla_nom_p.txt";

        public PGroupExtractor() : base(FileName)
        {
        }
    }
}
