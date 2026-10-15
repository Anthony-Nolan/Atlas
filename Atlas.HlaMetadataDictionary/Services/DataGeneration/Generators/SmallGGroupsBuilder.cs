using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.GeneticData.Hla.Services.AlleleNameUtils;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Utils.Extensions;
using Atlas.HlaMetadataDictionary.Extensions;
using Atlas.HlaMetadataDictionary.HlaTypingInfo;
using Atlas.HlaMetadataDictionary.InternalModels.HLATypings;
using Atlas.HlaMetadataDictionary.Repositories;
using Atlas.HlaMetadataDictionary.WmdaDataAccess.Models;

namespace Atlas.HlaMetadataDictionary.Services.DataGeneration.Generators
{
    /// <summary>
    /// Like a P group, a "small g" group consists of alleles that have the same protein sequence at the ABD region,
    /// but null-alleles are also included when grouping.
    /// </summary>
    internal interface ISmallGGroupsBuilder
    {
        /// <returns>
        /// All small g groups - with or without 'g' suffix - and single alleles that do not share an ABD protein
        /// sequence with any other allele.
        /// </returns>
        IEnumerable<SmallGGroup> BuildSmallGGroups(string hlaNomenclatureVersion);
    }

    internal class SmallGGroupsBuilder : ISmallGGroupsBuilder
    {
        private readonly Dictionary<string, IReadOnlyCollection<SmallGGroup>> smallGGroupCollections
            = new Dictionary<string, IReadOnlyCollection<SmallGGroup>>();

        private readonly IWmdaDataRepository wmdaDataRepository;

        public SmallGGroupsBuilder(IWmdaDataRepository wmdaDataRepository)
        {
            this.wmdaDataRepository = wmdaDataRepository;
        }

        /// <summary>
        /// Note, in the absence of an IMGT/HLA file, small g group metadata must be generated from P and G group info.
        /// </summary>
        public IEnumerable<SmallGGroup> BuildSmallGGroups(string hlaNomenclatureVersion)
        {
            if (hlaNomenclatureVersion.IsNullOrEmpty())
            {
                throw new ArgumentNullException(nameof(hlaNomenclatureVersion));
            }

            if (!smallGGroupCollections.TryGetValue(hlaNomenclatureVersion, out var dataset))
            {
                var wmdaDataset = wmdaDataRepository.GetWmdaDataset(hlaNomenclatureVersion);

                dataset = BuildSmallGGroupsFromPGroups(wmdaDataset)
                    .Concat(BuildSmallGGroupsFromNullAllelesNotMappedToPGroups(wmdaDataset))
                    .ToList();

                smallGGroupCollections.Add(hlaNomenclatureVersion, dataset);
            }

            return dataset;
        }

        private static IEnumerable<SmallGGroup> BuildSmallGGroupsFromPGroups(WmdaDataset wmdaDataset)
        {
            // Indexed once here: every P group looks up the G groups that share one of its alleles.
            var gGroupIndexesByAllele = wmdaDataset.GGroups
                .SelectMany((gGroup, index) => gGroup.Alleles.Select(allele => (Key: (gGroup.TypingLocus, allele), Index: index)))
                .ToLookup(x => x.Key, x => x.Index);

            return wmdaDataset.PGroups.Select(p => BuildSmallGGroupFromSinglePGroup(wmdaDataset.GGroups, gGroupIndexesByAllele, p));
        }

        private static SmallGGroup BuildSmallGGroupFromSinglePGroup(
            IList<HlaNomG> gGroups,
            ILookup<(string TypingLocus, string Allele), int> gGroupIndexesByAllele,
            IWmdaAlleleGroup pGroup)
        {
            // Each G group once, in the order of the dataset, as when every G group was searched.
            var nullAlleles = pGroup.Alleles
                .SelectMany(allele => gGroupIndexesByAllele[(pGroup.TypingLocus, allele)])
                .Distinct()
                .Order()
                .SelectMany(index => gGroups[index].Alleles.Where(ExpressionSuffixParser.IsAlleleNull));

            var allAlleles = pGroup.Alleles.Concat(nullAlleles).ToList();

            var groupName = AllelesShareSameFirstTwoFields(allAlleles)
                ? AlleleSplitter.FirstTwoFieldsAsString(allAlleles.First())
                : pGroup.Name.Replace('P', 'g');

            return new SmallGGroup
            {
                Locus = GetLocus(pGroup.TypingLocus),
                Name = groupName,
                Alleles = allAlleles,
                PGroup = pGroup.Name
            };
        }

        private static IEnumerable<SmallGGroup> BuildSmallGGroupsFromNullAllelesNotMappedToPGroups(WmdaDataset wmdaDataset)
        {
            // A set, so that each G group is checked without searching all the P group alleles.
            var pGroupAlleles = wmdaDataset.PGroups.SelectMany(p => p.GetAlleleNamesWithLocus()).ToHashSet();

            return wmdaDataset.GGroups
                .Where(g => !g.GetAlleleNamesWithLocus().Any(pGroupAlleles.Contains))
                .GroupBy(g => g.TypingLocus)
                .SelectMany(grp => BuildSmallGGroupsFromNullAllelesOfASingleLocus(grp.Key, grp));
        }

        private static IEnumerable<SmallGGroup> BuildSmallGGroupsFromNullAllelesOfASingleLocus(
            string locus,
            IEnumerable<IWmdaAlleleGroup> gGroups)
        {
            return gGroups
                .SelectMany(g => g.Alleles)
                .GroupBy(AlleleSplitter.FirstTwoFieldsAsString)
                .Select(groupedAlleles => new SmallGGroup
                {
                    Locus = GetLocus(locus),
                    Name = groupedAlleles.Count() == 1
                        ? AlleleSplitter.FirstTwoFieldsWithExpressionSuffixAsString(groupedAlleles.Single())
                        : groupedAlleles.Key,
                    Alleles = groupedAlleles.ToList()
                });
        }

        private static bool AllelesShareSameFirstTwoFields(IEnumerable<string> alleles)
        {
            return alleles.Select(AlleleSplitter.FirstTwoFieldsAsString).Distinct().Count() == 1;
        }

        private static Locus GetLocus(string locus)
        {
            return HlaMetadataDictionaryLoci.GetLocusFromTypingLocusNameIfExists(TypingMethod.Molecular, locus);
        }
    }
}