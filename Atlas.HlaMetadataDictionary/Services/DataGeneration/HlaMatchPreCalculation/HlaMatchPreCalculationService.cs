using Atlas.MatchingAlgorithm.MatchingDictionary.Models.MatchingTypings;
using Atlas.MatchingAlgorithm.MatchingDictionary.Repositories;
using Atlas.MatchingAlgorithm.MatchingDictionary.Services.HlaMatchPreCalculation;
using System.Collections.Generic;
using System.Linq;
using Atlas.MatchingAlgorithm.MatchingDictionary.Services.DataGeneration.HlaMatchPreCalculation;

namespace Atlas.MatchingAlgorithm.MatchingDictionary.Services
{
    /// <summary>
    /// Creates a complete collection of matched HLA
    /// by orchestrating the generation and compilation of matching info extracted from the WMDA files.
    /// </summary>
    public interface IHlaMatchPreCalculationService
    {
        IEnumerable<IMatchedHla> GetMatchedHla(string hlaDatabaseVersion);
    }

    public class HlaMatchPreCalculationService : IHlaMatchPreCalculationService
    {
        private readonly IWmdaDataRepository dataRepository;

        public HlaMatchPreCalculationService(IWmdaDataRepository dataRepository)
        {
            this.dataRepository = dataRepository;
        }

        public IEnumerable<IMatchedHla> GetMatchedHla(string hlaDatabaseVersion)
        {
            var hlaInfo = GetHlaInfoForMatching(hlaDatabaseVersion);
            var hlaMatchers = new List<IHlaMatcher>{ new AlleleMatcher(), new SerologyMatcher() };
            var matchedHla = PreCalculateMatchedHla(hlaMatchers, hlaInfo);

            return matchedHla;
        }

        private HlaInfoForMatching GetHlaInfoForMatching(string hlaDatabaseVersion)
        {
            var alleleInfoGenerator = new AlleleInfoGenerator(dataRepository);
            var alleleInfoForMatching = alleleInfoGenerator.GetAlleleInfoForMatching(hlaDatabaseVersion).ToList();
            var serologyInfoForMatching = new SerologyInfoGenerator(dataRepository).GetSerologyInfoForMatching(hlaDatabaseVersion).ToList();
            var alleleToSerologyRelationships = dataRepository.GetWmdaDataset(hlaDatabaseVersion).AlleleToSerologyRelationships.ToList();

            return new HlaInfoForMatching(alleleInfoForMatching, serologyInfoForMatching, alleleToSerologyRelationships);
        }

        private static IEnumerable<IMatchedHla> PreCalculateMatchedHla(IEnumerable<IHlaMatcher> hlaMatchers, HlaInfoForMatching hlaInfo)
        {
            return hlaMatchers.AsParallel().SelectMany(matcher => matcher.PreCalculateMatchedHla(hlaInfo)).ToList();
        }
    }
}
