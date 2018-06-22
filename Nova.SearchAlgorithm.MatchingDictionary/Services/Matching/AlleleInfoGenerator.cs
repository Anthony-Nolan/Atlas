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
        private readonly List<HlaNom> alleles;
        private readonly List<ConfidentialAllele> confidentialAlleles;
        private readonly List<AlleleStatus> alleleStatuses;
        private readonly List<HlaNomP> pGroups;
        private readonly List<HlaNomG> gGroups;

        public AlleleInfoGenerator(IWmdaDataRepository dataRepository)
        {
            // enumerating data collections once here, as they are accessed thousands of times
            alleles = dataRepository.Alleles.ToList();
            confidentialAlleles = dataRepository.ConfidentialAlleles.ToList();
            alleleStatuses = dataRepository.AlleleStatuses.ToList();
            pGroups = dataRepository.PGroups.ToList();
            gGroups = dataRepository.GGroups.ToList();
        }

        public IEnumerable<IAlleleInfoForMatching> GetAlleleInfoForMatching()
        {           
            var alleleInfo = alleles
                .Where(AlleleIsNotConfidential)
                .Select(GetInfoForSingleAllele);

            return alleleInfo;
        }

        private bool AlleleIsNotConfidential(IWmdaHlaTyping allele)
        {
            return !confidentialAlleles.Contains(allele);
        }

        private IAlleleInfoForMatching GetInfoForSingleAllele(HlaNom allele)
        {
            var alleleTyping = GetAlleleTyping(allele);

            var usedInMatching = allele.IdenticalHla.Equals("")
                ? alleleTyping
                : GetAlleleTypingFromIdenticalHla(allele);

            var pGroup = GetPGroup(usedInMatching);
            var gGroup = GetGGroup(usedInMatching);

            return new AlleleInfoForMatching(alleleTyping, usedInMatching, pGroup, gGroup);
        }

        private AlleleTyping GetAlleleTypingFromIdenticalHla(HlaNom allele)
        {
            var identicalHla = new HlaNom(TypingMethod.Molecular, allele.Locus, allele.IdenticalHla);
            return GetAlleleTyping(identicalHla);
        }

        private AlleleTyping GetAlleleTyping(HlaNom allele)
        {
            var alleleStatus = GetAlleleTypingStatus(allele);
            return new AlleleTyping(allele.Locus, allele.Name, alleleStatus, allele.IsDeleted);
        }

        private AlleleTypingStatus GetAlleleTypingStatus(IWmdaHlaTyping allele)
        {
            var alleleStatus = alleleStatuses
                .FirstOrDefault(status => status.TypingEquals(allele));

            return alleleStatus.ToAlleleTypingStatus();
        }

        private IEnumerable<string> GetPGroup(IWmdaHlaTyping allele)
        {
            return GetAlleleGroup(pGroups, allele);
        }

        private IEnumerable<string> GetGGroup(IWmdaHlaTyping allele)
        {
            return GetAlleleGroup(gGroups, allele);
        }

        private static IEnumerable<string> GetAlleleGroup(IEnumerable<IWmdaAlleleGroup> alleleGroups, IWmdaHlaTyping allele)
        {
            var alleleGroup = alleleGroups
                .Where(group => group.LocusEquals(allele))
                .SingleOrDefault(group => group.Alleles.Contains(allele.Name))
                ?.Name;

            return alleleGroup != null ? new List<string> { alleleGroup } : new List<string>();
        }
    }
}
