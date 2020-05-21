using Atlas.Common.GeneticData.Hla.Models;
using Atlas.HlaMetadataDictionary.Models.HLATypings;
using Atlas.HlaMetadataDictionary.Models.MatchingTypings;
using Atlas.HlaMetadataDictionary.Models.Wmda;
using Atlas.HlaMetadataDictionary.Repositories;
using Atlas.HlaMetadataDictionary.Services.DataGeneration.HlaMatchPreCalculation.SerologyRelationships;
using System.Collections.Generic;
using System.Linq;

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

        public IEnumerable<ISerologyInfoForMatching> GetSerologyInfoForMatching(string hlaDatabaseVersion)
        {
            return dataRepository.GetWmdaDataset(hlaDatabaseVersion).Serologies.Select(s => GetInfoForSingleSerology(s, hlaDatabaseVersion));
        }

        private ISerologyInfoForMatching GetInfoForSingleSerology(HlaNom serology, string hlaDatabaseVersion)
        {
            var serologyTyping = GetSerologyTyping(serology, hlaDatabaseVersion);
            var usedInMatching = GetTypingUsedInMatching(serology, serologyTyping, hlaDatabaseVersion);
            var matchingSerologies = GetAllMatchingSerologies(serologyTyping, usedInMatching, hlaDatabaseVersion);

            return new SerologyInfoForMatching(
                serologyTyping,
                usedInMatching,
                matchingSerologies);
        }

        private SerologyTyping GetSerologyTyping(HlaNom serology, string hlaDatabaseVersion)
        {
            var serologyRelationships = dataRepository.GetWmdaDataset(hlaDatabaseVersion).SerologyToSerologyRelationships;
            var family = new SerologyFamily(serologyRelationships, serology, serology.IsDeleted);
            return family.SerologyTyping;
        }

        private SerologyTyping GetTypingUsedInMatching(HlaNom serology, SerologyTyping serologyTyping, string hlaDatabaseVersion)
        {
            if (string.IsNullOrEmpty(serology.IdenticalHla))
            {
                return serologyTyping;
            }

            var identicalSerology = new HlaNom(TypingMethod.Serology, serology.TypingLocus, serology.IdenticalHla);
            return GetSerologyTyping(identicalSerology, hlaDatabaseVersion);
        }

        private IEnumerable<MatchingSerology> GetAllMatchingSerologies(
            SerologyTyping serologyTyping,
            SerologyTyping usedInMatching,
            string hlaDatabaseVersion)
        {
            var matchedToSerologyTyping = GetMatchingSerologies(serologyTyping, hlaDatabaseVersion);
            var matchedToUsedInMatching = GetMatchingSerologies(usedInMatching, hlaDatabaseVersion);

            return matchedToSerologyTyping.Union(matchedToUsedInMatching);
        }

        private IEnumerable<MatchingSerology> GetMatchingSerologies(SerologyTyping serology, string hlaDatabaseVersion)
        {
            var serologyRelationships = dataRepository.GetWmdaDataset(hlaDatabaseVersion).SerologyToSerologyRelationships;

            var calculator = new MatchingSerologyCalculatorFactory()
                .GetMatchingSerologyCalculator(serology.SerologySubtype, serologyRelationships);

            var family = new SerologyFamily(serologyRelationships, serology, serology.IsDeleted);

            return calculator.GetMatchingSerologies(family);
        }
    }
}