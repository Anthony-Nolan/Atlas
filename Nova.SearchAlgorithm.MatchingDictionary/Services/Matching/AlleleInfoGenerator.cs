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
            var confidentialAllelesList = dataRepository.ConfidentialAlleles.ToList();

            var alleleInfo = dataRepository.Alleles
                .Where(allele => !confidentialAllelesList.Contains(allele as IWmdaHlaTyping))
                .Select(allele => GetInfoForSingleAllele(allele, dataRepository));

            return alleleInfo;
        }

        private static IAlleleInfoForMatching GetInfoForSingleAllele(HlaNom alleleHlaNom, IWmdaDataRepository dataRepository)
        {
            var alleleStatus = GetAlleleTypingStatus(dataRepository.AlleleStatuses, alleleHlaNom);
            var allele = new AlleleTyping(alleleHlaNom.Locus, alleleHlaNom.Name, alleleStatus, alleleHlaNom.IsDeleted);

            var usedInMatching = alleleHlaNom.IdenticalHla.Equals("")
                ? allele
                : GetUsedInMatchingValueFromIdenticalHla(alleleHlaNom, dataRepository.AlleleStatuses);
            var pGroup = GetAlleleGroup(dataRepository.PGroups, usedInMatching);
            var gGroup = GetAlleleGroup(dataRepository.GGroups, usedInMatching);

            return new AlleleInfoForMatching(allele, usedInMatching, pGroup, gGroup);
        }

        private static AlleleTypingStatus GetAlleleTypingStatus(IEnumerable<AlleleStatus> alleleStatuses, IWmdaHlaTyping allele)
        {
            var alleleStatus = alleleStatuses
                .FirstOrDefault(status =>
                    status.Locus.Equals(allele.Locus) && status.Name.Equals(allele.Name));

            return alleleStatus.ToAlleleTypingStatus();
        }

        private static AlleleTyping GetUsedInMatchingValueFromIdenticalHla(HlaNom alleleHlaNom, IEnumerable<AlleleStatus> alleleStatuses)
        {
            var identicalHla = new HlaNom(TypingMethod.Molecular, alleleHlaNom.Locus, alleleHlaNom.IdenticalHla);
            var typingStatusOfIdenticalHla = GetAlleleTypingStatus(alleleStatuses, identicalHla);
            return new AlleleTyping(identicalHla.Locus, identicalHla.Name, typingStatusOfIdenticalHla);
        }

        private static IEnumerable<string> GetAlleleGroup(IEnumerable<IWmdaAlleleGroup> allAlleleGroups, IWmdaHlaTyping allele)
        {
            var alleleGroup = allAlleleGroups
                .SingleOrDefault(group =>
                    group.Locus.Equals(allele.Locus) && group.Alleles.Contains(allele.Name)
                )?.Name;

            return alleleGroup != null ? new List<string> { alleleGroup } : new List<string>();
        }
    }
}
