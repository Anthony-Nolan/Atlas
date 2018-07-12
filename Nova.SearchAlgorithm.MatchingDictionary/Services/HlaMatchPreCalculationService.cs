using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Services.HlaMatchPreCalculation;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services
{
    /// <summary>
    /// Creates a complete collection of matched HLA
    /// by orchestrating the generation and compilation of matching info extracted from the WMDA files.
    /// </summary>
    public interface IHlaMatchPreCalculationService
    {
        IEnumerable<IMatchedHla> GetMatchedHla();
    }

    public class HlaMatchPreCalculationService : IHlaMatchPreCalculationService
    {
        private readonly IWmdaDataRepository dataRepository;

        public HlaMatchPreCalculationService(IWmdaDataRepository dataRepository)
        {
            this.dataRepository = dataRepository;
        }

        public IEnumerable<IMatchedHla> GetMatchedHla()
        {
            var hlaInfo = GetHlaInfoForMatching();
            var hlaMatchers = new List<IHlaMatcher>{ new AlleleMatcher(), new SerologyMatcher() };
            var matchedHla = PreCalculateMatchedHla(hlaMatchers, hlaInfo);

            return matchedHla;
        }

        private HlaInfoForMatching GetHlaInfoForMatching()
        {
            var alleleInfoForMatching =
                new AlleleInfoGenerator(dataRepository).GetAlleleInfoForMatching().ToList();
            var serologyInfoForMatching =
                new SerologyInfoGenerator(dataRepository).GetSerologyInfoForMatching().ToList();
            var alleleToSerologyRelationships = dataRepository.AlleleToSerologyRelationships.ToList();

            return new HlaInfoForMatching(alleleInfoForMatching, serologyInfoForMatching, alleleToSerologyRelationships);
        }

        private static IEnumerable<IMatchedHla> PreCalculateMatchedHla(IEnumerable<IHlaMatcher> hlaMatchers, HlaInfoForMatching hlaInfo)
        {
            return hlaMatchers.SelectMany(matcher => matcher.PreCalculateMatchedHla(hlaInfo));
        }
    }
}
