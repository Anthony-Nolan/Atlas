using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.HlaMetadataDictionary.Extensions;
using Atlas.HlaMetadataDictionary.InternalModels.Metadata;
using Atlas.HlaMetadataDictionary.InternalModels.MetadataTableRows;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;
using Atlas.MultipleAlleleCodeDictionary;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface;

namespace Atlas.HlaMetadataDictionary.Services.DataRetrieval
{
    /// <summary>
    ///  Consolidates TCE group assignments for DPB1 alleles.
    /// </summary>
    internal interface IDpb1TceGroupMetadataService : IHlaSearchingMetadataService<IDpb1TceGroupsMetadata>
    {
        Task<string> GetDpb1TceGroup(string dpb1HlaName, string hlaNomenclatureVersion);
    }

    internal class Dpb1TceGroupMetadataService : 
        HlaSearchingMetadataServiceBase<IDpb1TceGroupsMetadata>, 
        IDpb1TceGroupMetadataService
    {
        private const string NoTceGroupAssignment = "";

        public Dpb1TceGroupMetadataService(
            IDpb1TceGroupsMetadataRepository dpb1TceGroupsMetadataRepository,
            IAlleleNamesMetadataService alleleNamesMetadataService,
            IHlaCategorisationService hlaCategorisationService,
            IAlleleStringSplitterService alleleSplitter,
            IMacDictionary macDictionary
        ) : base(
            dpb1TceGroupsMetadataRepository,
            alleleNamesMetadataService,
            hlaCategorisationService,
            alleleSplitter,
            macDictionary
            )
        {
        }

        public async Task<string> GetDpb1TceGroup(string dpb1HlaName, string hlaNomenclatureVersion)
        {
            var metadata = await GetHlaMetadata(Locus.Dpb1, dpb1HlaName, hlaNomenclatureVersion);
            return metadata.TceGroup;
        }

        protected override IEnumerable<IDpb1TceGroupsMetadata> ConvertMetadataRowsToMetadata(
            IEnumerable<HlaMetadataTableRow> rows)
        {
            return rows.Select(row => row.ToDpb1TceGroupMetadata());
        }

        protected override IDpb1TceGroupsMetadata ConsolidateHlaMetadata(
            Locus locus,
            string lookupName,
            List<IDpb1TceGroupsMetadata> metadata)
        {
            var tceGroups = metadata
                .Select(data => data.TceGroup)
                .Distinct()
                .ToList();

            // If a DPB1 typing maps >1 TCE group, then it should be treated the same as an allele
            // that has no TCE group assignment.
            var tceGroup = tceGroups.Count == 1 ? tceGroups.Single() : NoTceGroupAssignment;

            return new Dpb1TceGroupsMetadata(lookupName, tceGroup);
        }
    }
}