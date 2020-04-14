using Atlas.MatchingAlgorithm.MatchingDictionary.Models.Wmda;

namespace Atlas.MatchingAlgorithm.MatchingDictionary.Repositories.WmdaExtractors.AlleleGroupExtractors
{
    internal class PGroupExtractor : AlleleGroupExtractorBase<HlaNomP>
    {
        private const string FileName = "hla_nom_p.txt";

        public PGroupExtractor() : base(FileName)
        {
        }
    }
}
