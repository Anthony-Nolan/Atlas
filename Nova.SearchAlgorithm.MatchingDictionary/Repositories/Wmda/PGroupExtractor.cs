using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.Wmda
{
    internal class PGroupExtractor : AlleleGroupExtractorBase<HlaNomP>
    {
        private const string FileName = "hla_nom_p.txt";

        public PGroupExtractor() : base(FileName)
        {
        }
    }
}
