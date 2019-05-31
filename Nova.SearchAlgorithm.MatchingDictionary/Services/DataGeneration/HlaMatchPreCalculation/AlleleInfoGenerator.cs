using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.HlaMatchPreCalculation
{
    /// <summary>
    /// Pulls together data from different WMDA files
    /// required for matching on single allele typings.
    /// </summary>
    internal class AlleleInfoGenerator
    {
        private readonly IWmdaDataRepository dataRepository;

        private readonly Dictionary<string, CachedAlleleInfo> AlleleInfo = new Dictionary<string, CachedAlleleInfo>();

        public AlleleInfoGenerator(IWmdaDataRepository dataRepository)
        {
            this.dataRepository = dataRepository;
        }

        public IEnumerable<IAlleleInfoForMatching> GetAlleleInfoForMatching(string hlaDatabaseVersion)
        {
            var alleleInformation = GetAlleleInfo(hlaDatabaseVersion);

            var nonConfidentialAlleles = alleleInformation.Alleles.Where(a => AlleleIsNotConfidential(a, hlaDatabaseVersion));

            return nonConfidentialAlleles.AsParallel().Select(a => GetInfoForSingleAllele(a, hlaDatabaseVersion)).ToList();
        }

        private CachedAlleleInfo GetAlleleInfo(string hlaDatabaseVersion)
        {
            if (!AlleleInfo.TryGetValue(hlaDatabaseVersion, out var alleleInformation))
            {
                alleleInformation = new CachedAlleleInfo
                {
                    // enumerating data collections once here, as they are accessed thousands of times
                    Alleles = dataRepository.GetWmdaDataset(hlaDatabaseVersion).Alleles.ToList(),
                    ConfidentialAlleles = dataRepository.GetWmdaDataset(hlaDatabaseVersion).ConfidentialAlleles.ToList(),
                    AlleleStatuses = dataRepository.GetWmdaDataset(hlaDatabaseVersion).AlleleStatuses.ToList(),
                    PGroups = dataRepository.GetWmdaDataset(hlaDatabaseVersion).PGroups.ToList(),
                    GGroups = dataRepository.GetWmdaDataset(hlaDatabaseVersion).GGroups.ToList(),
                };
                AlleleInfo.Add(hlaDatabaseVersion, alleleInformation);
            }

            return alleleInformation;
        }

        private bool AlleleIsNotConfidential(IWmdaHlaTyping allele, string hlaDatabaseVersion)
        {
            return !GetAlleleInfo(hlaDatabaseVersion).ConfidentialAlleles.Contains(allele);
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
            var alleleStatus = GetAlleleInfo(hlaDatabaseVersion).AlleleStatuses
                .FirstOrDefault(status => status.TypingEquals(allele));

            return alleleStatus.ToAlleleTypingStatus();
        }

        private IEnumerable<string> GetPGroup(IWmdaHlaTyping allele, string hlaDatabaseVersion)
        {
            return GetAlleleGroup(GetAlleleInfo(hlaDatabaseVersion).PGroups, allele);
        }

        private IEnumerable<string> GetGGroup(IWmdaHlaTyping allele, string hlaDatabaseVersion)
        {
            return GetAlleleGroup(GetAlleleInfo(hlaDatabaseVersion).GGroups, allele);
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

    internal class CachedAlleleInfo
    {
        public List<HlaNom> Alleles { get; set; }
        public List<ConfidentialAllele> ConfidentialAlleles { get; set; }
        public List<AlleleStatus> AlleleStatuses { get; set; }
        public List<HlaNomP> PGroups { get; set; }
        public List<HlaNomG> GGroups { get; set; }
    }
}