using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;

namespace Atlas.HlaMetadataDictionary.Services.DataRetrieval
{
    /// <summary>
    /// Handles matching HLA lookup logic at the locus-level,
    /// including handling of null-expressing alleles within the typing.
    /// </summary>
    internal interface ILocusHlaMatchingMetadataService
    {
        Task<LocusInfo<IHlaMatchingMetadata>> GetHlaMatchingMetadata(
            Locus locus,
            LocusInfo<string> locusTyping,
            string hlaNomenclatureVersion);
    }

    /// <inheritdoc />
    internal class LocusHlaMatchingMetadataService : ILocusHlaMatchingMetadataService
    {
        private readonly IHlaMatchingMetadataService singleHlaMetadataService;

        public LocusHlaMatchingMetadataService(IHlaMatchingMetadataService singleHlaMetadataService)
        {
            this.singleHlaMetadataService = singleHlaMetadataService;
        }

        public async Task<LocusInfo<IHlaMatchingMetadata>> GetHlaMatchingMetadata(
            Locus locus,
            LocusInfo<string> locusTyping,
            string hlaNomenclatureVersion)
        {
            var locusMetadata = await GetLocusMetadata(locus, locusTyping, hlaNomenclatureVersion);

            var result1 = HandleNullAlleles(locusMetadata[0], locusMetadata[1]);
            var result2 = HandleNullAlleles(locusMetadata[1], locusMetadata[0]);

            return new LocusInfo<IHlaMatchingMetadata>(result1, result2);
        }

        private async Task<IHlaMatchingMetadata[]> GetLocusMetadata(
            Locus locus,
            LocusInfo<string> locusHlaTyping,
            string hlaNomenclatureVersion)
        {
            return await Task.WhenAll(
                singleHlaMetadataService.GetHlaMetadata(locus, locusHlaTyping.Position1, hlaNomenclatureVersion),
                singleHlaMetadataService.GetHlaMetadata(locus, locusHlaTyping.Position2, hlaNomenclatureVersion));
        }

        private static IHlaMatchingMetadata HandleNullAlleles(
            IHlaMatchingMetadata metadata,
            IHlaMatchingMetadata otherMetadata)
        {
            return metadata.IsNullExpressingTyping
                ? MergeMatchingHla(metadata, otherMetadata)
                : metadata;
        }

        private static IHlaMatchingMetadata MergeMatchingHla(
            IHlaMatchingMetadata metadata,
            IHlaMatchingMetadata otherMetadata)
        {
            var mergedPGroups = metadata.MatchingPGroups.Union(otherMetadata.MatchingPGroups).ToList();

            // TODO: ATLAS-749: Find a less flakey way to do this. 
            var mergedLookupName = $"{metadata.LookupName}[NULL-AS]{otherMetadata.LookupName}";
            
            return new HlaMatchingMetadata(
                metadata.Locus,
                mergedLookupName,
                metadata.TypingMethod,
                mergedPGroups
                );
        }
    }
}