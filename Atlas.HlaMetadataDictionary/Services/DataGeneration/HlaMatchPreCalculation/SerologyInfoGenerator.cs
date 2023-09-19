using Atlas.Common.GeneticData.Hla.Models;
using Atlas.HlaMetadataDictionary.Repositories;
using Atlas.HlaMetadataDictionary.Services.DataGeneration.HlaMatchPreCalculation.SerologyRelationships;
using System.Collections.Generic;
using System.Linq;
using Atlas.HlaMetadataDictionary.InternalModels.HLATypings;
using Atlas.HlaMetadataDictionary.InternalModels.MatchingTypings;
using Atlas.HlaMetadataDictionary.WmdaDataAccess.Models;

namespace Atlas.HlaMetadataDictionary.Services.DataGeneration.HlaMatchPreCalculation
{
    /// <summary>
    /// Pulls together data from different WMDA files
    /// required for matching on serology typings.
    /// </summary>
    internal class SerologyInfoGenerator
    {
        private readonly IWmdaDataRepository dataRepository;

        public SerologyInfoGenerator(IWmdaDataRepository dataRepository)
        {
            this.dataRepository = dataRepository;
        }

        public IEnumerable<SerologyInfoForMatching> GetSerologyInfoForMatching(string hlaNomenclatureVersion)
        {
            return dataRepository.GetWmdaDataset(hlaNomenclatureVersion).Serologies.Select(s => GetInfoForSingleSerology(s, hlaNomenclatureVersion));
        }

        private SerologyInfoForMatching GetInfoForSingleSerology(HlaNom serology, string hlaNomenclatureVersion)
        {
            var serologyTyping = GetSerologyTyping(serology, hlaNomenclatureVersion);
            var usedInMatching = GetTypingUsedInMatching(serology, serologyTyping, hlaNomenclatureVersion);
            var matchingSerologies = GetAllMatchingSerologies(serologyTyping, usedInMatching, hlaNomenclatureVersion);

            return new SerologyInfoForMatching(
                serologyTyping,
                usedInMatching,
                matchingSerologies);
        }

        private SerologyTyping GetSerologyTyping(HlaNom serology, string hlaNomenclatureVersion)
        {
            var serologyRelationships = dataRepository.GetWmdaDataset(hlaNomenclatureVersion).SerologyToSerologyRelationships;
            var family = new SerologyFamily(serologyRelationships, serology, serology.IsDeleted);
            return family.SerologyTyping;
        }

        private SerologyTyping GetTypingUsedInMatching(HlaNom serology, SerologyTyping serologyTyping, string hlaNomenclatureVersion)
        {
            if (string.IsNullOrEmpty(serology.IdenticalHla))
            {
                return serologyTyping;
            }

            var identicalSerology = new HlaNom(TypingMethod.Serology, serology.TypingLocus, serology.IdenticalHla);
            return GetSerologyTyping(identicalSerology, hlaNomenclatureVersion);
        }

        private IEnumerable<MatchingSerology> GetAllMatchingSerologies(
            SerologyTyping serologyTyping,
            SerologyTyping usedInMatching,
            string hlaNomenclatureVersion)
        {
            var matchedToSerologyTyping = GetMatchingSerologies(serologyTyping, hlaNomenclatureVersion);
            var matchedToUsedInMatching = GetMatchingSerologies(usedInMatching, hlaNomenclatureVersion);

            return matchedToSerologyTyping.Union(matchedToUsedInMatching);
        }

        private IEnumerable<MatchingSerology> GetMatchingSerologies(SerologyTyping serology, string hlaNomenclatureVersion)
        {
            var serologyRelationships = dataRepository.GetWmdaDataset(hlaNomenclatureVersion).SerologyToSerologyRelationships;

            var calculator = new MatchingSerologyCalculatorFactory()
                .GetMatchingSerologyCalculator(serology.SerologySubtype, serologyRelationships);

            var family = new SerologyFamily(serologyRelationships, serology, serology.IsDeleted);

            return calculator.GetMatchingSerologies(family);
        }
    }
}