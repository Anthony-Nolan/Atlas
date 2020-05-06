using System.Collections.Generic;
using System.Linq;
using Atlas.HlaMetadataDictionary.Models.HLATypings;
using Atlas.HlaMetadataDictionary.Models.MatchingTypings;
using Atlas.HlaMetadataDictionary.Models.Wmda;
using Atlas.HlaMetadataDictionary.Repositories;

namespace Atlas.HlaMetadataDictionary.Services.HlaMatchPreCalculation
{
    /// <summary>
    /// Pulls together data from different WMDA files
    /// required for matching on single allele typings.
    /// </summary>
    internal class AlleleInfoGenerator
    {
        private readonly IWmdaDataRepository dataRepository;

        public AlleleInfoGenerator(IWmdaDataRepository dataRepository)
        {
            this.dataRepository = dataRepository;
        }

        public IEnumerable<IAlleleInfoForMatching> GetAlleleInfoForMatching(string hlaDatabaseVersion)
        {
            var alleles = dataRepository.GetWmdaDataset(hlaDatabaseVersion).Alleles;

            var nonConfidentialAlleles = alleles.Where(a => AlleleIsNotConfidential(a, hlaDatabaseVersion));

            return nonConfidentialAlleles.AsParallel().Select(a => GetInfoForSingleAllele(a, hlaDatabaseVersion)).ToList();
        }

        private bool AlleleIsNotConfidential(IWmdaHlaTyping allele, string hlaDatabaseVersion)
        {
            return !dataRepository.GetWmdaDataset(hlaDatabaseVersion).ConfidentialAlleles.Contains(allele);
        }

        private IAlleleInfoForMatching GetInfoForSingleAllele(HlaNom allele, string hlaDatabaseVersion)
        {
            var alleleTyping = GetAlleleTyping(allele, hlaDatabaseVersion);

            var usedInMatching = allele.IdenticalHla.Equals("")
                ? alleleTyping
                : GetAlleleTypingFromIdenticalHla(allele, hlaDatabaseVersion);

            var pGroup = GetPGroup(usedInMatching, hlaDatabaseVersion);
            var gGroup = GetGGroup(usedInMatching, hlaDatabaseVersion);

            return new AlleleInfoForMatching(alleleTyping, usedInMatching, pGroup, gGroup);
        }

        private AlleleTyping GetAlleleTypingFromIdenticalHla(HlaNom allele, string hlaDatabaseVersion)
        {
            var identicalHla = new HlaNom(TypingMethod.Molecular, allele.TypingLocus, allele.IdenticalHla);
            return GetAlleleTyping(identicalHla, hlaDatabaseVersion);
        }

        private AlleleTyping GetAlleleTyping(HlaNom allele, string hlaDatabaseVersion)
        {
            var alleleStatus = GetAlleleTypingStatus(allele, hlaDatabaseVersion);
            return new AlleleTyping(allele.TypingLocus, allele.Name, alleleStatus, allele.IsDeleted);
        }

        private AlleleTypingStatus GetAlleleTypingStatus(IWmdaHlaTyping allele, string hlaDatabaseVersion)
        {
            var alleleStatus = dataRepository.GetWmdaDataset(hlaDatabaseVersion).AlleleStatuses
                .FirstOrDefault(status => status.TypingEquals(allele));

            return alleleStatus.ToAlleleTypingStatus();
        }

        private IEnumerable<string> GetPGroup(IWmdaHlaTyping allele, string hlaDatabaseVersion)
        {
            return GetAlleleGroup(dataRepository.GetWmdaDataset(hlaDatabaseVersion).PGroups, allele);
        }

        private IEnumerable<string> GetGGroup(IWmdaHlaTyping allele, string hlaDatabaseVersion)
        {
            return GetAlleleGroup(dataRepository.GetWmdaDataset(hlaDatabaseVersion).GGroups, allele);
        }

        private static IEnumerable<string> GetAlleleGroup(IEnumerable<IWmdaAlleleGroup> alleleGroups, IWmdaHlaTyping allele)
        {
            var alleleGroup = alleleGroups
                .Where(group => group.LocusEquals(allele))
                .SingleOrDefault(group => group.Alleles.Contains(allele.Name))
                ?.Name;

            return alleleGroup != null ? new List<string> {alleleGroup} : new List<string>();
        }
    }
}