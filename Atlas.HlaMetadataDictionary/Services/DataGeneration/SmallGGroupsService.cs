﻿using System.Collections.Generic;
using System.Linq;
using Atlas.HlaMetadataDictionary.Extensions;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Atlas.HlaMetadataDictionary.InternalModels.HLATypings;
using Atlas.HlaMetadataDictionary.InternalModels.Metadata;

namespace Atlas.HlaMetadataDictionary.Services.DataGeneration
{
    /// <summary>
    /// Generates a complete collection of Small G Group Metadata, where allele lookup name
    /// is mapped to its corresponding small g group name.
    /// </summary>
    internal interface ISmallGGroupsService
    {
        IEnumerable<ISmallGGroupsMetadata> GetSmallGGroupMetadata(string hlaNomenclatureVersion);
    }

    internal class SmallGGroupsService : ISmallGGroupsService
    {
        private readonly ISmallGGroupsBuilder smallGGroupsBuilder;

        public SmallGGroupsService(ISmallGGroupsBuilder smallGGroupsBuilder)
        {
            this.smallGGroupsBuilder = smallGGroupsBuilder;
        }

        public IEnumerable<ISmallGGroupsMetadata> GetSmallGGroupMetadata(string hlaNomenclatureVersion)
        {
            var smallGGroups = smallGGroupsBuilder.BuildSmallGGroups(hlaNomenclatureVersion);
            var allMetadata = smallGGroups
                .SelectMany(BuildMetadata)
                .SelectMany(GetMetadataPerLookupName);

            return GroupMetadataByLookupName(allMetadata);
        }

        private static IEnumerable<ISmallGGroupsMetadata> BuildMetadata(SmallGGroup smallGGroup)
        {
            return smallGGroup.Alleles.Select(a => new SmallGGroupsMetadata(smallGGroup.Locus, a, smallGGroup.Name));
        }

        private static IEnumerable<ISmallGGroupsMetadata> GetMetadataPerLookupName(ISmallGGroupsMetadata smallGGroup)
        {
            var groupName = smallGGroup.SmallGGroups.Single();

            var lookupNames = GetLookupNames(smallGGroup);

            return lookupNames.Select(lookupName => new SmallGGroupsMetadata(smallGGroup.Locus, lookupName, groupName));
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
                .GroupBy(result => new { result.Locus, result.LookupName })
                .Select(grp =>
                    new SmallGGroupsMetadata(
                        grp.Key.Locus,
                        grp.Key.LookupName, 
                        grp.SelectMany(g => g.SmallGGroups).Distinct().ToList()
                        ));
        }
    }
}