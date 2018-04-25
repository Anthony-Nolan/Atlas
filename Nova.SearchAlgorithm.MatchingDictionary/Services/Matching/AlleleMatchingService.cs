using System;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Data.Wmda;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.Matching
{
    public interface IAlleleMatchingService
    {
        IEnumerable<IMatchingPGroups> MatchAllelesToPGroups(Func<IWmdaHlaType, bool> filter);
    }

    public class AlleleMatchingService : IAlleleMatchingService
    {
        private readonly IWmdaRepository _repository;

        public AlleleMatchingService(IWmdaRepository repo)
        {
            _repository = repo;
        }

        public IEnumerable<IMatchingPGroups> MatchAllelesToPGroups(Func<IWmdaHlaType, bool> filter)
        {
            var allAlleles = WmdaDataFactory.GetData<HlaNom>(_repository, filter);
            var confidentialAlleles = WmdaDataFactory.GetData<Confidential>(_repository, filter);
            var pGroups = WmdaDataFactory.GetData<HlaNomP>(_repository, filter);

            var allMatching = allAlleles
                .Where(a => !confidentialAlleles.Contains(a as IWmdaHlaType))
                .Select(a => GetSingleMatchingAllele(pGroups, a));

            return allMatching;
        }

        private static IMatchingPGroups GetSingleMatchingAllele(IEnumerable<HlaNomP> allPGroups, HlaNom hlaNom)
        {
            var allele = new Allele(hlaNom.WmdaLocus, hlaNom.Name, hlaNom.IsDeleted);

            var usedInMatching = !hlaNom.IdenticalHla.Equals("")
                    ? new Allele(hlaNom.WmdaLocus, hlaNom.IdenticalHla)
                    : new Allele(allele);

            return new AlleleToPGroup(allele, usedInMatching, GetPGroup(allPGroups, usedInMatching));
        }

        private static IEnumerable<string> GetPGroup(IEnumerable<HlaNomP> allPGroups, IWmdaHlaType allele)
        {
            var pGroup = allPGroups.SingleOrDefault(p =>
                p.WmdaLocus.Equals(allele.WmdaLocus)
                && p.Alleles.Contains(allele.Name)
                )?.Name;

            return pGroup != null ? new List<string> { pGroup } : new List<string>();
        }
    }
}
