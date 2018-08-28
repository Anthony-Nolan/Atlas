using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.Wmda
{
    internal class GGroupExtractor : AlleleGroupExtractorBase<HlaNomG>
    {
        private const string FileName = "hla_nom_g.txt";

        public GGroupExtractor() : base(FileName)
        {
        }
    }
}
