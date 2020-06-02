using System.Collections.Generic;
using System.Linq;
using Atlas.HlaMetadataDictionary.InternalModels.MatchingTypings;
using Atlas.HlaMetadataDictionary.Repositories;

namespace Atlas.HlaMetadataDictionary.Services.DataGeneration.HlaMatchPreCalculation
{
    /// <summary>
    /// Creates a complete collection of matched HLA
    /// by orchestrating the generation and compilation of matching info extracted from the WMDA files.
    /// </summary>
    internal interface IHlaMatchPreCalculationService
    {
        IEnumerable<IMatchedHla> GetMatchedHla(string hlaNomenclatureVersion);
    }

    internal class HlaMatchPreCalculationService : IHlaMatchPreCalculationService
    {
        private readonly IWmdaDataRepository dataRepository;

        public HlaMatchPreCalculationService(IWmdaDataRepository dataRepository)
        {
            this.dataRepository = dataRepository;
        }

        public IEnumerable<IMatchedHla> GetMatchedHla(string hlaNomenclatureVersion)
        {
            var hlaInfo = GetHlaInfoForMatching(hlaNomenclatureVersion);
            var hlaMatchers = new List<IHlaMatcher>{ new AlleleMatcher(), new SerologyMatcher() };
            var matchedHla = PreCalculateMatchedHla(hlaMatchers, hlaInfo);

            return matchedHla;
        }

        private HlaInfoForMatching GetHlaInfoForMatching(string hlaNomenclatureVersion)
        {
            var alleleInfoGenerator = new AlleleInfoGenerator(dataRepository);
            var alleleInfoForMatching = alleleInfoGenerator.GetAlleleInfoForMatching(hlaNomenclatureVersion).ToList();
            var serologyInfoForMatching = new SerologyInfoGenerator(dataRepository).GetSerologyInfoForMatching(hlaNomenclatureVersion).ToList();
            var alleleToSerologyRelationships = dataRepository.GetWmdaDataset(hlaNomenclatureVersion).AlleleToSerologyRelationships.ToList();

            return new HlaInfoForMatching(alleleInfoForMatching, serologyInfoForMatching, alleleToSerologyRelationships);
        }

        private static IEnumerable<IMatchedHla> PreCalculateMatchedHla(IEnumerable<IHlaMatcher> hlaMatchers, HlaInfoForMatching hlaInfo)
        {
            return hlaMatchers.AsParallel().SelectMany(matcher => matcher.PreCalculateMatchedHla(hlaInfo)).ToList();
        }
    }
}
