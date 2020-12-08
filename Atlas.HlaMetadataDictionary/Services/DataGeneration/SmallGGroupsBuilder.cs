using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.GeneticData.Hla.Services.AlleleNameUtils;
using Atlas.HlaMetadataDictionary.Extensions;
using Atlas.HlaMetadataDictionary.HlaTypingInfo;
using Atlas.HlaMetadataDictionary.InternalModels.HLATypings;
using Atlas.HlaMetadataDictionary.Repositories;
using Atlas.HlaMetadataDictionary.WmdaDataAccess.Models;

namespace Atlas.HlaMetadataDictionary.Services.DataGeneration
{
    /// <summary>
    /// Like a P group, a "small g" group consists of alleles that have the same protein sequence at the ARS region,
    /// but null-alleles are also included when grouping.
    /// </summary>
    internal interface ISmallGGroupsBuilder
    {
        /// <returns>
        /// All small g groups - with or without 'g' suffix - and single alleles that do not share an ARS protein
        /// sequence with any other allele.
        /// </returns>
        IEnumerable<SmallGGroup> BuildSmallGGroups(string hlaNomenclatureVersion);
    }

    internal class SmallGGroupsBuilder : ISmallGGroupsBuilder
    {
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
            var wmdaDataset = wmdaDataRepository.GetWmdaDataset(hlaNomenclatureVersion);

            return BuildSmallGGroupsFromPGroups(wmdaDataset)
                .Concat(BuildSmallGGroupsFromNullAllelesNotMappedToPGroups(wmdaDataset));
        }

        private static IEnumerable<SmallGGroup> BuildSmallGGroupsFromPGroups(WmdaDataset wmdaDataset)
        {
            return wmdaDataset.PGroups.Select(p => BuildSmallGGroupFromSinglePGroup(wmdaDataset.GGroups, p));
        }

        private static SmallGGroup BuildSmallGGroupFromSinglePGroup(IEnumerable<HlaNomG> gGroups, IWmdaAlleleGroup pGroup)
        {
            var nullAlleles = gGroups
                .Where(g => g.TypingLocus == pGroup.TypingLocus && g.Alleles.Intersect(pGroup.Alleles).Any())
                .SelectMany(g => g.Alleles.Where(ExpressionSuffixParser.IsAlleleNull));

            var allAlleles = pGroup.Alleles.Concat(nullAlleles).ToList();

            var groupName = AllelesShareSameFirstTwoFields(allAlleles)
                ? AlleleSplitter.FirstTwoFieldsAsString(allAlleles.First())
                : pGroup.Name.Replace('P', 'g');

            return new SmallGGroup
            {
                Locus = GetLocus(pGroup.TypingLocus),
                Name = groupName,
                Alleles = allAlleles
            };
        }

        private static IEnumerable<SmallGGroup> BuildSmallGGroupsFromNullAllelesNotMappedToPGroups(WmdaDataset wmdaDataset)
        {
            var pGroupAlleles = wmdaDataset.PGroups.SelectMany(p => p.GetAlleleNamesWithLocus()).ToList();

            return wmdaDataset.GGroups
                .Where(g => !g.GetAlleleNamesWithLocus().Intersect(pGroupAlleles).Any())
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
                    Name = groupedAlleles.Count() == 1 ? groupedAlleles.Single() : groupedAlleles.Key,
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