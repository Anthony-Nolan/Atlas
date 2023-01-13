using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.Extensions;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Atlas.HlaMetadataDictionary.InternalModels.HLATypings;
using Atlas.HlaMetadataDictionary.InternalModels.MatchingTypings;
using Atlas.HlaMetadataDictionary.InternalModels.Metadata;

namespace Atlas.HlaMetadataDictionary.Services.DataGeneration.Generators
{
    /// <summary>
    /// Generates a complete collection of metadata related to small G groups.
    /// </summary>
    internal interface ISmallGGroupsService
    {
        /// <returns>Small g group(s) for each possible allele lookup name.</returns>
        IEnumerable<ISmallGGroupsMetadata> GetSmallGGroupsMetadata(
            string hlaNomenclatureVersion,
            IEnumerable<IHlaMetadataSource<SerologyTyping>> serologyTypings);

        /// <returns>P group mapping for each small g group.</returns>
        IEnumerable<IMolecularTypingToPGroupMetadata> GetSmallGGroupToPGroupMetadata(string hlaNomenclatureVersion);
    }

    internal class SmallGGroupsService : ISmallGGroupsService
    {
        private readonly ISmallGGroupsBuilder smallGGroupsBuilder;

        public SmallGGroupsService(ISmallGGroupsBuilder smallGGroupsBuilder)
        {
            this.smallGGroupsBuilder = smallGGroupsBuilder;
        }

        public IEnumerable<ISmallGGroupsMetadata> GetSmallGGroupsMetadata(
            string hlaNomenclatureVersion,
            IEnumerable<IHlaMetadataSource<SerologyTyping>> serologyTypings)
        {
            var smallGGroups = smallGGroupsBuilder.BuildSmallGGroups(hlaNomenclatureVersion).ToList();
            var molecularMetadata = smallGGroups
                .SelectMany(BuildMolecularMetadata)
                .SelectMany(GetMolecularMetadataPerLookupName);

            var pGroupToSmallGGroup = BuildPGroupToSmallGGroupLookup(smallGGroups);

            var serologyMetadata = serologyTypings
                .Select(serology => BuildSerologyMetadata(serology, pGroupToSmallGGroup));

            return GroupMetadataByLookupName(molecularMetadata.Concat(serologyMetadata));
        }

        private static LociInfo<Dictionary<string, string>> BuildPGroupToSmallGGroupLookup(List<SmallGGroup> smallGGroups)
        {
            var pGroupToSmallGGroup = new LociInfo<Dictionary<string, string>>(_ => new Dictionary<string, string>());

            foreach (var smallGGroup in smallGGroups)
            {
                var locus = smallGGroup.Locus;
                var pGroup = smallGGroup.PGroup;
                var name = smallGGroup.Name;

                if (pGroup == null)
                {
                    continue;
                }

                var perLocusLookup = pGroupToSmallGGroup.GetLocus(locus);
                if (perLocusLookup.ContainsKey(pGroup))
                {
                    if (perLocusLookup[pGroup] != name)
                    {
                        throw new Exception("Found two g-groups corresponding to one g-group. This should not be possible.");
                    }
                }
                else
                {
                    perLocusLookup[pGroup] = name;
                }
            }

            return pGroupToSmallGGroup;
        }

        private static SmallGGroupsMetadata BuildSerologyMetadata(
            IHlaMetadataSource<SerologyTyping> serology,
            LociInfo<Dictionary<string, string>> pGroupToSmallGGroup)
        {
            var locus = serology.TypingForHlaMetadata.Locus;
            var serologyName = serology.TypingForHlaMetadata.Name;

            var perLocusLookup = pGroupToSmallGGroup.GetLocus(locus);
            var smallGGroups = serology.MatchingPGroups.Select(p => perLocusLookup[p]).ToHashSet().ToList();

            return new SmallGGroupsMetadata(
                locus,
                serologyName,
                TypingMethod.Serology,
                smallGGroups
            );
        }

        public IEnumerable<IMolecularTypingToPGroupMetadata> GetSmallGGroupToPGroupMetadata(string hlaNomenclatureVersion)
        {
            var smallGGroups = smallGGroupsBuilder.BuildSmallGGroups(hlaNomenclatureVersion);

            return smallGGroups.Select(g => new MolecularTypingToPGroupMetadata(g.Locus, g.Name, g.PGroup));
        }

        private static IEnumerable<ISmallGGroupsMetadata> BuildMolecularMetadata(SmallGGroup smallGGroup)
        {
            return smallGGroup.Alleles.Select(a => new SmallGGroupsMetadata(smallGGroup.Locus, a, TypingMethod.Molecular, smallGGroup.Name));
        }

        private static IEnumerable<ISmallGGroupsMetadata> GetMolecularMetadataPerLookupName(ISmallGGroupsMetadata smallGGroup)
        {
            var groupName = smallGGroup.SmallGGroups.Single();

            var lookupNames = GetLookupNames(smallGGroup);

            return lookupNames.Select(lookupName => new SmallGGroupsMetadata(smallGGroup.Locus, lookupName, TypingMethod.Molecular, groupName));
        }

        private static IEnumerable<string> GetLookupNames(IHlaMetadata smallGGroup)
        {
            var allele = new AlleleTyping(smallGGroup.Locus, smallGGroup.LookupName);
            return new[]
                {
                    allele.Name,
                    allele.ToXxCodeLookupName()
                }
                .Concat(allele.ToNmdpCodeAlleleLookupNames());
        }

        private static IEnumerable<ISmallGGroupsMetadata> GroupMetadataByLookupName(
            IEnumerable<ISmallGGroupsMetadata> results)
        {
            return results
                .GroupBy(result => new {result.Locus, result.LookupName, result.TypingMethod})
                .Select(grp =>
                    new SmallGGroupsMetadata(
                        grp.Key.Locus,
                        grp.Key.LookupName,
                        grp.Key.TypingMethod,
                        grp.SelectMany(g => g.SmallGGroups).Distinct().ToList()
                    ));
        }
    }
}