using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.HlaMetadataDictionary.Extensions;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings;
using Atlas.HlaMetadataDictionary.InternalModels.Metadata;
using Atlas.HlaMetadataDictionary.Repositories;
using Atlas.HlaMetadataDictionary.WmdaDataAccess.Models;

namespace Atlas.HlaMetadataDictionary.Services.DataGeneration.Generators
{
    /// <summary>
    /// Generates a complete collection of DPB1 TCE Group Metadata.
    /// </summary>
    internal interface IDpb1TceGroupsService
    {
        IEnumerable<IDpb1TceGroupsMetadata> GetDpb1TceGroupMetadata(string hlaNomenclatureVersion);
    }

    /// <inheritdoc />
    /// <summary>
    /// Extracts DPB1 TCE groups from a WMDA data repository.
    /// </summary>
    internal class Dpb1TceGroupsService : IDpb1TceGroupsService
    {
        private readonly IWmdaDataRepository wmdaDataRepository;

        public Dpb1TceGroupsService(IWmdaDataRepository wmdaDataRepository)
        {
            this.wmdaDataRepository = wmdaDataRepository;
        }

        public IEnumerable<IDpb1TceGroupsMetadata> GetDpb1TceGroupMetadata(string hlaNomenclatureVersion)
        {
            var allMetadata = wmdaDataRepository
                .GetWmdaDataset(hlaNomenclatureVersion)
                .Dpb1TceGroupAssignments
                .SelectMany(GetMetadataPerDpb1LookupName);

            return GroupMetadataByLookupName(allMetadata);
        }

        private static IEnumerable<IDpb1TceGroupsMetadata> GetMetadataPerDpb1LookupName(Dpb1TceGroupAssignment tceGroupAssignment)
        {
            var lookupNames = GetLookupNames(tceGroupAssignment);

            return lookupNames.Select(name => new Dpb1TceGroupsMetadata(name, tceGroupAssignment.VersionTwoAssignment));
        }

        private static IEnumerable<string> GetLookupNames(IWmdaHlaTyping tceGroup)
        {
            var allele = new AlleleTyping(Locus.Dpb1, tceGroup.Name);
            return new[]
            {
                allele.Name,
                allele.ToXxCodeLookupName()
            }
            .Concat(allele.ToNmdpCodeAlleleLookupNames());
        }

        /// <summary>
        /// Due to DPB1 nomenclature, DPB1* expressing alleles with the same lookup name
        /// e.g., [0-9]+:XX, will all have the same protein, and thus the same TCE group.
        /// If a group of alleles with the same lookup name contains a null allele, the assignment
        /// of the expressing alleles should be preferred.
        /// </summary>
        private static IEnumerable<IDpb1TceGroupsMetadata> GroupMetadataByLookupName(
            IEnumerable<IDpb1TceGroupsMetadata> results)
        {
            return results
                .GroupBy(result => result.LookupName)
                .Select(grp => new Dpb1TceGroupsMetadata(
                    grp.Key,
                    grp.Select(lookup => lookup.TceGroup)
                        .Distinct()
                        .OrderByDescending(tceGroup => tceGroup)
                        .First()));
        }
    }
}
