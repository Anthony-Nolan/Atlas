using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Services.HlaMatchPreCalculation.SerologyRelationships;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.HlaMatchPreCalculation
{
    /// <summary>
    /// Pulls together data from different WMDA files
    /// required for matching on serology typings.
    /// </summary>
    internal class SerologyInfoGenerator
    {
        private readonly IEnumerable<HlaNom> serologies;
        private readonly List<RelSerSer> serologyRelationships;

        public SerologyInfoGenerator(IWmdaDataRepository dataRepository)
        {
            serologies = dataRepository.Serologies;

            // enumerating data collection here as it will be access hundreds of times
            serologyRelationships = dataRepository.SerologyToSerologyRelationships.ToList();
        }

        public IEnumerable<ISerologyInfoForMatching> GetSerologyInfoForMatching()
        {
            return serologies.Select(GetInfoForSingleSerology);
        }

        private ISerologyInfoForMatching GetInfoForSingleSerology(HlaNom serology)
        {
            var serologyTyping = GetSerologyTyping(serology);
            var usedInMatching = GetTypingUsedInMatching(serology, serologyTyping);
            var matchingSerologies = GetAllMatchingSerologies(serologyTyping, usedInMatching);

            return new SerologyInfoForMatching(
                serologyTyping, 
                usedInMatching, 
                matchingSerologies);
        }

        private SerologyTyping GetSerologyTyping(HlaNom serology)
        {
            var family = new SerologyFamily(serologyRelationships, serology, serology.IsDeleted);
            return family.SerologyTyping;
        }

        private SerologyTyping GetTypingUsedInMatching(HlaNom serology, SerologyTyping serologyTyping)
        {
            if (string.IsNullOrEmpty(serology.IdenticalHla))
            {
                return serologyTyping;
            }

            var identicalSerology = new HlaNom(TypingMethod.Serology, serology.Locus, serology.IdenticalHla);
            return GetSerologyTyping(identicalSerology);
        }

        private IEnumerable<MatchingSerology> GetAllMatchingSerologies(
            SerologyTyping serologyTyping,
            SerologyTyping usedInMatching)
        {
            var matchedToSerologyTyping = GetMatchingSerologies(serologyTyping);
            var matchedToUsedInMatching = GetMatchingSerologies(usedInMatching);

            return matchedToSerologyTyping.Union(matchedToUsedInMatching);
        }

        private IEnumerable<MatchingSerology> GetMatchingSerologies(SerologyTyping serology)
        {
            var calculator = new MatchingSerologyCalculatorFactory()
                .GetMatchingSerologyCalculator(serology.SerologySubtype, serologyRelationships);

            var family = new SerologyFamily(serologyRelationships, serology, serology.IsDeleted);

            return calculator.GetMatchingSerologies(family);
        }
    }
}
