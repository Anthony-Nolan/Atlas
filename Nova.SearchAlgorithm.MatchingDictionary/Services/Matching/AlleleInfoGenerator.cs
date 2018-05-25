using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.Matching
{
    /// <summary>
    /// Pulls together data from different WMDA files
    /// required for matching on single allele typings.
    /// </summary>
    internal class AlleleInfoGenerator
    {
        public IEnumerable<IAlleleInfoForMatching> GetAlleleInfoForMatching(IWmdaDataRepository dataRepository)
        {
            var alleleInfo = dataRepository.HlaNomAlleles
                .Where(allele => !dataRepository.ConfidentialAlleles.Contains(allele as IWmdaHlaTyping))
                .Select(allele => GetInfoForSingleAllele(allele, dataRepository.HlaNomP, dataRepository.HlaNomG));

            return alleleInfo;
        }

        private static IAlleleInfoForMatching GetInfoForSingleAllele(HlaNom alleleHlaNom, IEnumerable<HlaNomP> allPGroups, IEnumerable<HlaNomG> allGGroups)
        {
            var allele = new AlleleTyping(alleleHlaNom.WmdaLocus, alleleHlaNom.Name, alleleHlaNom.IsDeleted);

            var usedInMatching = !alleleHlaNom.IdenticalHla.Equals("")
                    ? new AlleleTyping(alleleHlaNom.WmdaLocus, alleleHlaNom.IdenticalHla)
                    : new AlleleTyping(allele);

            var pGroup = GetAlleleGroup(allPGroups, usedInMatching);
            var gGroup = GetAlleleGroup(allGGroups, usedInMatching);

            return new AlleleInfoForMatching(allele, usedInMatching, pGroup, gGroup);
        }

        private static IEnumerable<string> GetAlleleGroup(IEnumerable<IWmdaAlleleGroup> allAlleleGroups, IWmdaHlaTyping allele)
        {
            var alleleGroup = allAlleleGroups
                .SingleOrDefault(group => 
                    group.WmdaLocus.Equals(allele.WmdaLocus) && group.Alleles.Contains(allele.Name)
                )?.Name;

            return alleleGroup != null ? new List<string> { alleleGroup } : new List<string>();
        }
    }
}
