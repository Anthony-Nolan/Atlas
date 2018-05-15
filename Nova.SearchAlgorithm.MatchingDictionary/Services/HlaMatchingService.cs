using System;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Data.Wmda;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Services.Matching;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services
{
    public interface IHlaMatchingService
    {
        IEnumerable<IMatchedHla> MatchAllHla(
            Func<IWmdaHlaType, bool> serologyFilter, Func<IWmdaHlaType, bool> molecularFilter);
    }

    public class HlaMatchingService : IHlaMatchingService
    {
        private readonly IWmdaRepository _repository;
        private readonly IAlleleMatchingService _alleleMatchingService;
        private readonly ISerologyMatchingService _serologyMatchingService;

        public HlaMatchingService(
            IWmdaRepository repo,
            IAlleleMatchingService alleleMatchingService,
            ISerologyMatchingService serologyMatchingService)
        {
            _repository = repo;
            _alleleMatchingService = alleleMatchingService;
            _serologyMatchingService = serologyMatchingService;
        }

        public IEnumerable<IMatchedHla> MatchAllHla(
            Func<IWmdaHlaType, bool> serologyFilter, Func<IWmdaHlaType, bool> molecularFilter)
        {
            var allelesToPGroups = _alleleMatchingService.MatchAllelesToPGroups(molecularFilter).ToList();
            var serologyToSerology = _serologyMatchingService.MatchSerologyToSerology(serologyFilter).ToList();
            var relDnaSer = WmdaDataFactory.GetData<RelDnaSer>(_repository, molecularFilter).ToList();

            var matchedAlleles = new AlleleToSerologyMatching().MatchAllelesToSerology(allelesToPGroups, serologyToSerology, relDnaSer);
            var matchedSerology = new SerologyToPGroupsMatching().MatchSerologyToAlleles(allelesToPGroups, serologyToSerology, relDnaSer);

            var allMatchingHla = new List<IMatchedHla>();
            allMatchingHla.AddRange(matchedAlleles);
            allMatchingHla.AddRange(matchedSerology);

            return allMatchingHla;
        }        
    }
}
