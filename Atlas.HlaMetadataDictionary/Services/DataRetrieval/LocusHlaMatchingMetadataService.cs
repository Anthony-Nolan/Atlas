using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;

namespace Atlas.HlaMetadataDictionary.Services.DataRetrieval
{
    /// <summary>
    /// Handles matching HLA lookup logic at the locus-level,
    /// including handling of null-expressing alleles within the typing.
    /// </summary>
    internal interface ILocusHlaMatchingMetadataService
    {
        Task<LocusInfo<INullHandledHlaMatchingMetadata>> GetHlaMatchingMetadata(
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

        public async Task<LocusInfo<INullHandledHlaMatchingMetadata>> GetHlaMatchingMetadata(
            Locus locus,
            LocusInfo<string> locusTyping,
            string hlaNomenclatureVersion)
        {
            var locusMetadata = await GetLocusMetadata(locus, locusTyping, hlaNomenclatureVersion);

            var result1 = HandleNullAlleles(locusMetadata[0], locusMetadata[1]);
            var result2 = HandleNullAlleles(locusMetadata[1], locusMetadata[0]);

            return new LocusInfo<INullHandledHlaMatchingMetadata>(result1, result2);
        }

        private async Task<IHlaMatchingMetadata[]> GetLocusMetadata(
            Locus locus,
            LocusInfo<string> locusHlaTyping,
            string hlaNomenclatureVersion)
        {
            return await Task.WhenAll(
                GetLocusMetadataPerPosition(locus, locusHlaTyping.Position1, hlaNomenclatureVersion),
                GetLocusMetadataPerPosition(locus, locusHlaTyping.Position2, hlaNomenclatureVersion));
        }

        private static INullHandledHlaMatchingMetadata HandleNullAlleles(IHlaMatchingMetadata metadata, IHlaMatchingMetadata otherMetadata)
        {
            return metadata.IsNullExpressingTyping
                ? MergeMatchingHla(metadata, otherMetadata)
                : new NullHandledHlaMatchingMetadata(metadata);
        }

        private static INullHandledHlaMatchingMetadata MergeMatchingHla(IHlaMatchingMetadata metadata, IHlaMatchingMetadata otherMetadata)
        {
            var mergedPGroups = metadata.MatchingPGroups.Union(otherMetadata.MatchingPGroups).ToList();
            var mergedLookupName = NullAlleleHandling.CombineAlleleNames(metadata.LookupName, otherMetadata.LookupName);

            return new NullHandledHlaMatchingMetadata(metadata, mergedLookupName, mergedPGroups);
        }

        private async Task<IHlaMatchingMetadata> GetLocusMetadataPerPosition(
            Locus locus,
            string locusHlaTyping,
            string hlaNomenclatureVersion)
        {
            return await singleHlaMetadataService.GetHlaMetadata(locus, locusHlaTyping, hlaNomenclatureVersion);
        }
    }
}