using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Services.HlaMatchPreCalculation.SerologyRelationships;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.DataGeneration.HlaMatchPreCalculation
{
    /// <summary>
    /// Pulls together data from different WMDA files
    /// required for matching on serology typings.
    /// </summary>
    internal class SerologyInfoGenerator
    {
        private readonly IWmdaDataRepository dataRepository;

        private readonly Dictionary<string, WmdaSerologyInfo> wmdaSerologyInfos = new Dictionary<string, WmdaSerologyInfo>();

        public SerologyInfoGenerator(IWmdaDataRepository dataRepository)
        {
            this.dataRepository = dataRepository;
        }

        public IEnumerable<ISerologyInfoForMatching> GetSerologyInfoForMatching(string hlaDatabaseVersion)
        {
            return GetWmdaSerologyInfo(hlaDatabaseVersion).Serologies.Select(s => GetInfoForSingleSerology(s, hlaDatabaseVersion));
        }

        private WmdaSerologyInfo GetWmdaSerologyInfo(string hlaDatabaseVersion)
        {
            if (!wmdaSerologyInfos.TryGetValue(hlaDatabaseVersion, out var serologyInfo))
            {
                serologyInfo = new WmdaSerologyInfo
                {
                    // enumerating data collection here as it will be access hundreds of times
                    Serologies = dataRepository.GetWmdaDataset(hlaDatabaseVersion).Serologies.ToList(),
                    SerologyRelationships = dataRepository.GetWmdaDataset(hlaDatabaseVersion).SerologyToSerologyRelationships.ToList()
                };
                wmdaSerologyInfos.Add(hlaDatabaseVersion, serologyInfo);
            }

            return serologyInfo;
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
            var serologyRelationships = GetWmdaSerologyInfo(hlaDatabaseVersion).SerologyRelationships;
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
            var serologyRelationships = GetWmdaSerologyInfo(hlaDatabaseVersion).SerologyRelationships;

            var calculator = new MatchingSerologyCalculatorFactory()
                .GetMatchingSerologyCalculator(serology.SerologySubtype, serologyRelationships);

            var family = new SerologyFamily(serologyRelationships, serology, serology.IsDeleted);

            return calculator.GetMatchingSerologies(family);
        }
    }

    internal class WmdaSerologyInfo
    {
        public IEnumerable<HlaNom> Serologies { get; set; }
        public List<RelSerSer> SerologyRelationships { get; set; }
    }
}