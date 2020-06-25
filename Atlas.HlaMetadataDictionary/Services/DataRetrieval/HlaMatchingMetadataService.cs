using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Atlas.HlaMetadataDictionary.InternalModels.MetadataTableRows;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface;
using System.Collections.Generic;
using System.Linq;

namespace Atlas.HlaMetadataDictionary.Services.DataRetrieval
{
    /// <summary>
    ///  Consolidates HLA info used in matching for all alleles that map to the hla name.
    /// </summary>
    internal interface IHlaMatchingMetadataService : IHlaSearchingMetadataService<IHlaMatchingMetadata>
    {
        IEnumerable<string> GetAllPGroups(string hlaNomenclatureVersion);
    }

    internal class HlaMatchingMetadataService : 
        HlaSearchingMetadataServiceBase<IHlaMatchingMetadata>, 
        IHlaMatchingMetadataService
    {
        private readonly IHlaMatchingMetadataRepository typedMatchingRepository;

        public HlaMatchingMetadataService(
            IHlaMatchingMetadataRepository hlaMatchingMetadataRepository,
            IAlleleNamesMetadataService alleleNamesMetadataService,
            IHlaCategorisationService hlaCategorisationService,
            IAlleleStringSplitterService alleleSplitter,
            IMacDictionary macDictionary,
            IAlleleGroupMetadataService alleleGroupMetadataService
        ) : base(
            hlaMatchingMetadataRepository,
            alleleNamesMetadataService,
            hlaCategorisationService,
            alleleSplitter,
            macDictionary,
            alleleGroupMetadataService)
        {
            typedMatchingRepository = hlaMatchingMetadataRepository;
        }

        protected override IEnumerable<IHlaMatchingMetadata> ConvertMetadataRowsToMetadata(
            IEnumerable<HlaMetadataTableRow> rows)
        {
            return rows.Select(row => row.ToHlaMatchingMetadata());
        }

        protected override IHlaMatchingMetadata ConsolidateHlaMetadata(
            Locus locus,
            string lookupName,
            List<IHlaMatchingMetadata> metadata)
        {
            var typingMethod = metadata
                .First()
                .TypingMethod;

            var pGroups = metadata
                .SelectMany(data => data.MatchingPGroups)
                .Distinct();

            return new HlaMatchingMetadata(
                locus,
                lookupName,
                typingMethod,
                pGroups);
        }

        public IEnumerable<string> GetAllPGroups(string hlaNomenclatureVersion)
        {
            return typedMatchingRepository.GetAllPGroups(hlaNomenclatureVersion);
        }

    }
}