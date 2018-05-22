using System;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Data.Wmda;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Services.Matching;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services
{
    /// <summary>
    /// Creates a complete collection of matched HLA
    /// by orchestrating the generation and compilation of matching info extracted from the WMDA files.
    /// </summary>
    public interface IHlaMatchingService
    {
        IEnumerable<IMatchedHla> GetMatchedHla(
            Func<IWmdaHlaTyping, bool> serologyFilter, Func<IWmdaHlaTyping, bool> molecularFilter);
    }

    public class HlaMatchingService : IHlaMatchingService
    {
        private readonly IWmdaRepository _repository;

        public HlaMatchingService(IWmdaRepository repo)
        {
            _repository = repo;
        }

        public IEnumerable<IMatchedHla> GetMatchedHla(
            Func<IWmdaHlaTyping, bool> serologyFilter, Func<IWmdaHlaTyping, bool> molecularFilter)
        {
            var hlaInfo = GetHlaInfoForMatching(serologyFilter, molecularFilter);
            var hlaMatchers = new List<IHlaMatcher>{ new AlleleMatcher(), new SerologyMatcher() };
            var matchedHla = CreateMatchedHla(hlaMatchers, hlaInfo);

            return matchedHla;
        }

        private HlaInfoForMatching GetHlaInfoForMatching(
            Func<IWmdaHlaTyping, bool> serologyFilter, Func<IWmdaHlaTyping, bool> molecularFilter)
        {
            var alleleInfoForMatching =
                new AlleleInfoGenerator().GetAlleleInfoForMatching(_repository, molecularFilter).ToList();
            var serologyInfoForMatching =
                new SerologyInfoGenerator().GetSerologyInfoForMatching(_repository, serologyFilter).ToList();
            var relDnaSer = 
                WmdaDataFactory.GetData<RelDnaSer>(_repository, molecularFilter).ToList();

            return new HlaInfoForMatching(alleleInfoForMatching, serologyInfoForMatching, relDnaSer);
        }

        private static IEnumerable<IMatchedHla> CreateMatchedHla(IEnumerable<IHlaMatcher> hlaMatchers, HlaInfoForMatching hlaInfo)
        {
            return hlaMatchers.SelectMany(matcher => matcher.CreateMatchedHla(hlaInfo));
        }
    }
}
