using System.Threading.Tasks;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;

namespace Atlas.HlaMetadataDictionary.ExternalInterface
{
    public interface IHlaMetadataCacheControl
    {
        /// <summary>
        /// Force all in-memory caches to populate (used to achieve slow-start-up, rather than slow-first-request)
        /// </summary>
        Task PreWarmAllCaches();

        /// <summary>
        /// Force population of the in-memory cache of AlleleNames.
        /// </summary>
        Task PreWarmAlleleNameCache();

        /// <summary>
        /// Force population of the in-memory caches that live imputation reads for almost every subject: allele names,
        /// allele groups, small g groups, and the G group and small g group to P group lookups.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Cheap once the tables are cached: each is then one cache read. Until a table is cached, every lookup in it
        /// is a single-row Table Storage read, and those reads are limited per table while the whole table loads in
        /// the background - so a cold process converting many donors at once waits on them. Awaiting this first avoids that.
        /// </para>
        /// <para>
        /// The matching, DPB1 TCE group and serology to alleles tables are not loaded: match prediction does not read them.
        /// </para>
        /// <para>
        /// The scoring table is not loaded either, although match prediction does read it, but only in two cases: to find
        /// the G group of a null allele in a typing, and to convert a typing to G or P groups for a frequency set that
        /// holds haplotypes at those groups. With small g group frequency sets, only subjects with a null allele read it,
        /// so most processes never need it. The first such lookup is a single-row read, and it starts a load of the whole
        /// table in the background, as for any table that is not warmed. If frequency sets at G or P group are imported,
        /// every subject reads the table, and it should be added here.
        /// </para>
        /// </remarks>
        Task PreWarmMatchPredictionCaches();
    }

    internal class HlaMetadataCacheControl : IHlaMetadataCacheControl
    {
        private readonly string hlaNomenclatureVersion;

        private readonly IAlleleNamesMetadataRepository alleleNamesRepository;
        private readonly IAlleleGroupsMetadataRepository alleleGroupsRepository;
        private readonly IHlaMatchingMetadataRepository matchingMetadataRepository;
        private readonly IHlaScoringMetadataRepository scoringMetadataRepository;
        private readonly IDpb1TceGroupsMetadataRepository dpb1TceGroupsMetadataRepository;
        private readonly IGGroupToPGroupMetadataRepository gGroupToPGroupMetadataRepository;
        private readonly IHlaNameToSmallGGroupLookupRepository hlaNameToSmallGGroupLookupRepository;
        private readonly ISmallGGroupToPGroupMetadataRepository smallGGroupToPGroupMetadataRepository;
        private readonly ISerologyToAllelesMetadataRepository serologyToAllelesMetadataRepository;

        public HlaMetadataCacheControl(
            string hlaNomenclatureVersion,
            IAlleleNamesMetadataRepository alleleNamesRepository,
            IAlleleGroupsMetadataRepository alleleGroupsRepository,
            IHlaMatchingMetadataRepository matchingMetadataRepository,
            IHlaScoringMetadataRepository scoringMetadataRepository,
            IDpb1TceGroupsMetadataRepository dpb1TceGroupsMetadataRepository,
            IGGroupToPGroupMetadataRepository gGroupToPGroupMetadataRepository,
            IHlaNameToSmallGGroupLookupRepository hlaNameToSmallGGroupLookupRepository,
            ISmallGGroupToPGroupMetadataRepository smallGGroupToPGroupMetadataRepository,
            ISerologyToAllelesMetadataRepository serologyToAllelesMetadataRepository)
        {
            this.hlaNomenclatureVersion = hlaNomenclatureVersion;

            this.alleleNamesRepository = alleleNamesRepository;
            this.alleleGroupsRepository = alleleGroupsRepository;
            this.matchingMetadataRepository = matchingMetadataRepository;
            this.scoringMetadataRepository = scoringMetadataRepository;
            this.dpb1TceGroupsMetadataRepository = dpb1TceGroupsMetadataRepository;
            this.gGroupToPGroupMetadataRepository = gGroupToPGroupMetadataRepository;
            this.hlaNameToSmallGGroupLookupRepository = hlaNameToSmallGGroupLookupRepository;
            this.smallGGroupToPGroupMetadataRepository = smallGGroupToPGroupMetadataRepository;
            this.serologyToAllelesMetadataRepository = serologyToAllelesMetadataRepository;
        }

        public async Task PreWarmAllCaches()
        {
            await PreWarmAlleleNameCache();

            await matchingMetadataRepository.LoadDataIntoMemory(hlaNomenclatureVersion);
            await scoringMetadataRepository.LoadDataIntoMemory(hlaNomenclatureVersion);
            await dpb1TceGroupsMetadataRepository.LoadDataIntoMemory(hlaNomenclatureVersion);
            await gGroupToPGroupMetadataRepository.LoadDataIntoMemory(hlaNomenclatureVersion);
            await hlaNameToSmallGGroupLookupRepository.LoadDataIntoMemory(hlaNomenclatureVersion);
            await smallGGroupToPGroupMetadataRepository.LoadDataIntoMemory(hlaNomenclatureVersion);
            await serologyToAllelesMetadataRepository.LoadDataIntoMemory(hlaNomenclatureVersion);
        }

        public async Task PreWarmAlleleNameCache()
        {
            await alleleNamesRepository.LoadDataIntoMemory(hlaNomenclatureVersion);
        }

        /// <inheritdoc />
        public async Task PreWarmMatchPredictionCaches()
        {
            // Concurrently: the tables are independent, and the first warm of a process waits on all of them.
            await Task.WhenAll(
                alleleNamesRepository.LoadDataIntoMemory(hlaNomenclatureVersion),
                alleleGroupsRepository.LoadDataIntoMemory(hlaNomenclatureVersion),
                hlaNameToSmallGGroupLookupRepository.LoadDataIntoMemory(hlaNomenclatureVersion),
                smallGGroupToPGroupMetadataRepository.LoadDataIntoMemory(hlaNomenclatureVersion),
                gGroupToPGroupMetadataRepository.LoadDataIntoMemory(hlaNomenclatureVersion));
        }
    }
}
