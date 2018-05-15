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
    public class AlleleToPGroupMatcher
    {
        public IEnumerable<IAlleleToPGroup> MatchAllelesToPGroups(IWmdaRepository repo, Func<IWmdaHlaType, bool> filter)
        {
            var allAlleles = WmdaDataFactory.GetData<HlaNom>(repo, filter);
            var confidentialAlleles = WmdaDataFactory.GetData<Confidential>(repo, filter);
            var pGroups = WmdaDataFactory.GetData<HlaNomP>(repo, filter);

            var allMatching = allAlleles
                .Where(a => !confidentialAlleles.Contains(a as IWmdaHlaType))
                .Select(a => GetSingleMatchingAllele(pGroups, a));

            return allMatching;
        }

        private static IAlleleToPGroup GetSingleMatchingAllele(IEnumerable<HlaNomP> allPGroups, HlaNom hlaNom)
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
