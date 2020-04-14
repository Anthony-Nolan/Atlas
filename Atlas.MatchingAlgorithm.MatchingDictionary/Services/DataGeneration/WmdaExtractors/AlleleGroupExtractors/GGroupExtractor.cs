using Atlas.MatchingAlgorithm.MatchingDictionary.Models.Wmda;

namespace Atlas.MatchingAlgorithm.MatchingDictionary.Repositories.WmdaExtractors.AlleleGroupExtractors
{
    internal class GGroupExtractor : AlleleGroupExtractorBase<HlaNomG>
    {
        private const string FileName = "hla_nom_g.txt";

        public GGroupExtractor() : base(FileName)
        {
        }
    }
}
