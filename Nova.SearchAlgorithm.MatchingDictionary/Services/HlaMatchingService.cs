using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Services.Matching;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services
{
    /// <summary>
    /// Creates a complete collection of matched HLA
    /// by orchestrating the generation and compilation of matching info extracted from the WMDA files.
    /// </summary>
    public interface IHlaMatchingService
    {
        IEnumerable<IMatchedHla> GetMatchedHla();
    }

    public class HlaMatchingService : IHlaMatchingService
    {
        private readonly IWmdaDataRepository dataRepository;

        public HlaMatchingService(IWmdaDataRepository dataRepository)
        {
            this.dataRepository = dataRepository;
        }

        public IEnumerable<IMatchedHla> GetMatchedHla()
        {
            var hlaInfo = GetHlaInfoForMatching();
            var hlaMatchers = new List<IHlaMatcher>{ new AlleleMatcher(), new SerologyMatcher() };
            var matchedHla = CreateMatchedHla(hlaMatchers, hlaInfo);

            return matchedHla;
        }

        private HlaInfoForMatching GetHlaInfoForMatching()
        {
            var alleleInfoForMatching =
                new AlleleInfoGenerator(dataRepository).GetAlleleInfoForMatching().ToList();
            var serologyInfoForMatching =
                new SerologyInfoGenerator().GetSerologyInfoForMatching(dataRepository).ToList();
            var alleleToSerologyRelationships = dataRepository.AlleleToSerologyRelationships.ToList();

            return new HlaInfoForMatching(alleleInfoForMatching, serologyInfoForMatching, alleleToSerologyRelationships);
        }

        private static IEnumerable<IMatchedHla> CreateMatchedHla(IEnumerable<IHlaMatcher> hlaMatchers, HlaInfoForMatching hlaInfo)
        {
            return hlaMatchers.SelectMany(matcher => matcher.CreateMatchedHla(hlaInfo));
        }
    }
}
