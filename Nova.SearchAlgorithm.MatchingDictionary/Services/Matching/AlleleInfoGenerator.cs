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
    /// <summary>
    /// This class is responsible for 
    /// pulling together the data from different WMDA files
    /// that is required for matching on single allele typings.
    /// </summary>
    internal class AlleleInfoGenerator
    {
        public IEnumerable<IAlleleInfoForMatching> GetAlleleInfoForMatching(IWmdaRepository repo, Func<IWmdaHlaType, bool> filter)
        {
            var allAlleles = WmdaDataFactory.GetData<HlaNom>(repo, filter);
            var confidentialAlleles = WmdaDataFactory.GetData<Confidential>(repo, filter);
            var pGroups = WmdaDataFactory.GetData<HlaNomP>(repo, filter);

            var alleleInfo = allAlleles
                .Where(allele => !confidentialAlleles.Contains(allele as IWmdaHlaType))
                .Select(allele => GetInfoForSingleAllele(allele, pGroups));

            return alleleInfo;
        }

        private static IAlleleInfoForMatching GetInfoForSingleAllele(HlaNom alleleHlaNom, IEnumerable<HlaNomP> allPGroups)
        {
            var allele = new Allele(alleleHlaNom.WmdaLocus, alleleHlaNom.Name, alleleHlaNom.IsDeleted);

            var usedInMatching = !alleleHlaNom.IdenticalHla.Equals("")
                    ? new Allele(alleleHlaNom.WmdaLocus, alleleHlaNom.IdenticalHla)
                    : new Allele(allele);

            return new AlleleInfoForMatching(allele, usedInMatching, GetPGroup(allPGroups, usedInMatching));
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
