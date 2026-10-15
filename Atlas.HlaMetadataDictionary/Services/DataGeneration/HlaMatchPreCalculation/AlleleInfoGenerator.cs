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
            var dataset = dataRepository.GetWmdaDataset(hlaNomenclatureVersion);
            var lookups = new AlleleLookups(dataset);

            var nonConfidentialAlleles = dataset.Alleles.Where(a => !lookups.IsConfidential(a));

            return nonConfidentialAlleles.AsParallel().Select(a => GetInfoForSingleAllele(a, lookups)).ToList();
        }

        private static AlleleInfoForMatching GetInfoForSingleAllele(HlaNom allele, AlleleLookups lookups)
        {
            var alleleTyping = GetAlleleTyping(allele, lookups);

            var usedInMatching = allele.IdenticalHla.Equals("")
                ? alleleTyping
                : GetAlleleTypingFromIdenticalHla(allele, lookups);

            var pGroup = lookups.GetPGroup(usedInMatching);
            var gGroup = lookups.GetGGroup(usedInMatching);

            return new AlleleInfoForMatching(alleleTyping, usedInMatching, pGroup, gGroup);
        }

        private static AlleleTyping GetAlleleTypingFromIdenticalHla(HlaNom allele, AlleleLookups lookups)
        {
            var identicalHla = new HlaNom(TypingMethod.Molecular, allele.TypingLocus, allele.IdenticalHla);
            return GetAlleleTyping(identicalHla, lookups);
        }

        private static AlleleTyping GetAlleleTyping(HlaNom allele, AlleleLookups lookups)
        {
            var alleleStatus = lookups.GetAlleleStatus(allele);
            return new AlleleTyping(allele.TypingLocus, allele.Name, alleleStatus.ToAlleleTypingStatus(), allele.IsDeleted);
        }

        /// <summary>
        /// The WMDA data that is looked up for every allele, indexed by locus and name,
        /// so that each allele does not search the whole of the dataset.
        /// </summary>
        private class AlleleLookups
        {
            private readonly HashSet<(string TypingLocus, string Name)> confidentialAlleles;
            private readonly Dictionary<(string TypingLocus, string Name), AlleleStatus> alleleStatuses = new();
            private readonly ILookup<(string TypingLocus, string AlleleName), IWmdaAlleleGroup> pGroups;
            private readonly ILookup<(string TypingLocus, string AlleleName), IWmdaAlleleGroup> gGroups;

            public AlleleLookups(WmdaDataset dataset)
            {
                confidentialAlleles = dataset.ConfidentialAlleles.Select(a => (a.TypingLocus, a.Name)).ToHashSet();

                // The first status of an allele, as before.
                foreach (var status in dataset.AlleleStatuses)
                {
                    alleleStatuses.TryAdd((status.TypingLocus, status.Name), status);
                }

                pGroups = GroupsByAllele(dataset.PGroups);
                gGroups = GroupsByAllele(dataset.GGroups);
            }

            public bool IsConfidential(IWmdaHlaTyping allele) => confidentialAlleles.Contains((allele.TypingLocus, allele.Name));

            public AlleleStatus GetAlleleStatus(IWmdaHlaTyping allele) => alleleStatuses.GetValueOrDefault((allele.TypingLocus, allele.Name));

            public string GetPGroup(IWmdaHlaTyping allele) => GetAlleleGroup(pGroups, allele);

            public string GetGGroup(IWmdaHlaTyping allele) => GetAlleleGroup(gGroups, allele);

            /// <remarks>
            /// An allele must be in at most one group: <see cref="Enumerable.SingleOrDefault{TSource}(IEnumerable{TSource})"/> throws otherwise, as before.
            /// </remarks>
            private static string GetAlleleGroup(ILookup<(string, string), IWmdaAlleleGroup> groups, IWmdaHlaTyping allele) =>
                groups[(allele.TypingLocus, allele.Name)].SingleOrDefault()?.Name;

            private static ILookup<(string TypingLocus, string AlleleName), IWmdaAlleleGroup> GroupsByAllele(IEnumerable<IWmdaAlleleGroup> groups) =>
                groups
                    .SelectMany(group => group.Alleles.Distinct().Select(alleleName => (Key: (group.TypingLocus, alleleName), Group: group)))
                    .ToLookup(x => x.Key, x => x.Group);
        }
    }
}
