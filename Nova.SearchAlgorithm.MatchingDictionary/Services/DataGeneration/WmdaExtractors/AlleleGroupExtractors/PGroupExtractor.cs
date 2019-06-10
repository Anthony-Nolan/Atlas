using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.WmdaExtractors.AlleleGroupExtractors
{
    internal class PGroupExtractor : AlleleGroupExtractorBase<HlaNomP>
    {
        private const string FileName = "hla_nom_p.txt";

        public PGroupExtractor() : base(FileName)
        {
        }
    }
}
