using Atlas.Common.GeneticData.Hla.Models;
using Atlas.HlaMetadataDictionary.Repositories;
using System.Collections.Generic;
using System.Linq;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings;
using Atlas.HlaMetadataDictionary.InternalModels.MatchingTypings;
using Atlas.HlaMetadataDictionary.WmdaDataAccess.Models;

namespace Atlas.HlaMetadataDictionary.Services.DataGeneration.HlaMatchPreCalculation
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

        public IEnumerable<AlleleInfoForMatching> GetAlleleInfoForMatching(string hlaNomenclatureVersion)
        {
            var alleles = dataRepository.GetWmdaDataset(hlaNomenclatureVersion).Alleles;

            var nonConfidentialAlleles = alleles.Where(a => AlleleIsNotConfidential(a, hlaNomenclatureVersion));

            return nonConfidentialAlleles.AsParallel().Select(a => GetInfoForSingleAllele(a, hlaNomenclatureVersion)).ToList();
        }

        private bool AlleleIsNotConfidential(IWmdaHlaTyping allele, string hlaNomenclatureVersion)
        {
            return !dataRepository.GetWmdaDataset(hlaNomenclatureVersion).ConfidentialAlleles.Contains(allele);
        }

        private AlleleInfoForMatching GetInfoForSingleAllele(HlaNom allele, string hlaNomenclatureVersion)
        {
            var alleleTyping = GetAlleleTyping(allele, hlaNomenclatureVersion);

            var usedInMatching = allele.IdenticalHla.Equals("")
                ? alleleTyping
                : GetAlleleTypingFromIdenticalHla(allele, hlaNomenclatureVersion);

            var pGroup = GetPGroup(usedInMatching, hlaNomenclatureVersion);
            var gGroup = GetGGroup(usedInMatching, hlaNomenclatureVersion);

            return new AlleleInfoForMatching(alleleTyping, usedInMatching, pGroup, gGroup);
        }

        private AlleleTyping GetAlleleTypingFromIdenticalHla(HlaNom allele, string hlaNomenclatureVersion)
        {
            var identicalHla = new HlaNom(TypingMethod.Molecular, allele.TypingLocus, allele.IdenticalHla);
            return GetAlleleTyping(identicalHla, hlaNomenclatureVersion);
        }

        private AlleleTyping GetAlleleTyping(HlaNom allele, string hlaNomenclatureVersion)
        {
            var alleleStatus = GetAlleleTypingStatus(allele, hlaNomenclatureVersion);
            return new AlleleTyping(allele.TypingLocus, allele.Name, alleleStatus, allele.IsDeleted);
        }

        private AlleleTypingStatus GetAlleleTypingStatus(IWmdaHlaTyping allele, string hlaNomenclatureVersion)
        {
            var alleleStatus = dataRepository.GetWmdaDataset(hlaNomenclatureVersion).AlleleStatuses
                .FirstOrDefault(status => status.TypingEquals(allele));

            return alleleStatus.ToAlleleTypingStatus();
        }

        private string GetPGroup(IWmdaHlaTyping allele, string hlaNomenclatureVersion)
        {
            return GetAlleleGroup(dataRepository.GetWmdaDataset(hlaNomenclatureVersion).PGroups, allele);
        }

        private string GetGGroup(IWmdaHlaTyping allele, string hlaNomenclatureVersion)
        {
            return GetAlleleGroup(dataRepository.GetWmdaDataset(hlaNomenclatureVersion).GGroups, allele);
        }

        private static string GetAlleleGroup(IEnumerable<IWmdaAlleleGroup> alleleGroups, IWmdaHlaTyping allele)
        {
            var alleleGroup = alleleGroups
                .Where(group => group.LocusEquals(allele))
                .SingleOrDefault(group => group.Alleles.Contains(allele.Name))
                ?.Name;

            return alleleGroup;
        }
    }
}