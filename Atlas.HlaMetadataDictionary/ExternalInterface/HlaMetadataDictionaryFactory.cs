using System;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Caching;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;
using Atlas.HlaMetadataDictionary.Services.DataGeneration;
using Atlas.HlaMetadataDictionary.Services.DataRetrieval;
using Atlas.HlaMetadataDictionary.Services.HlaConversion;
using Atlas.HlaMetadataDictionary.Services.HlaValidation;
using Atlas.HlaMetadataDictionary.WmdaDataAccess;
using LazyCache;

namespace Atlas.HlaMetadataDictionary.ExternalInterface
{
    public interface IHlaMetadataDictionaryFactory
    {
        /// <summary>
        /// Returns an appropriate Dictionary or CacheControl, given the config settings.
        /// Calling code is responsible for ensuring that `activeHlaNomenclatureVersion` is updating per-request.
        /// </summary>
        /// <remarks>
        /// Returns from a cross-request PersistentCache if the HLA Nomenclature version is familiar.
        /// If not, builds a new appropriate Dictionary and CacheControl, stores them and returns them.
        /// </remarks>
        IHlaMetadataDictionary BuildDictionary(string activeHlaNomenclatureVersion);

        IHlaMetadataCacheControl BuildCacheControl(string activeHlaNomenclatureVersion);
    }

    /// <summary>
    /// Responsible for creating (and handling appropriate caching) of the entry points to the HlaMetadataDictionary library.
    /// </summary>
    /// <remarks>
    /// We expect the Factory itself to be Transient, but it gets a reference to a singleton Persistent Cache of the Dictionaries that it has made previously.
    /// It needs to be Transient so that it is using a new copy of the dependencies each time it tries to create a fresh object.
    /// </remarks>
    internal class HlaMetadataDictionaryFactory : IHlaMetadataDictionaryFactory
    {
        private readonly IAppCache cache;

        //For Dictionary
        private readonly IRecreateHlaMetadataService recreateMetadataService;
        private readonly IHlaConverter hlaConverter;
        private readonly IHlaValidator hlaValidator;
        private readonly IHlaMatchingMetadataService hlaMatchingMetadataService;
        private readonly ILocusHlaMatchingMetadataService locusHlaMatchingMetadataService;
        private readonly IHlaScoringMetadataService hlaScoringMetadataService;
        private readonly IDpb1TceGroupMetadataService dpb1TceGroupMetadataService;
        private readonly IGGroupToPGroupMetadataService gGroupToPGroupMetadataService;
        private readonly ISmallGGroupToPGroupMetadataService smallGGroupToPGroupMetadataService;
        private readonly ISerologyToAllelesMetadataService serologyToAllelesMetadataService;
        private readonly IAlleleNamesMetadataService alleleNamesMetadataService;
        private readonly IHlaMetadataGenerationOrchestrator hlaMetadataGenerationOrchestrator;
        private readonly IWmdaHlaNomenclatureVersionAccessor wmdaHlaNomenclatureVersionAccessor;
        private readonly ILogger logger;

        //For CacheControl
        private readonly IAlleleNamesMetadataRepository alleleNamesRepository;
        private readonly IHlaMatchingMetadataRepository matchingMetadataRepository;
        private readonly IHlaScoringMetadataRepository scoringMetadataRepository;
        private readonly IDpb1TceGroupsMetadataRepository dpb1TceGroupsMetadataRepository;
        private readonly IGGroupToPGroupMetadataRepository gGroupToPGroupMetadataRepository;
        private readonly IHlaNameToSmallGGroupLookupRepository hlaNameToSmallGGroupLookupRepository;
        private readonly ISmallGGroupToPGroupMetadataRepository smallGGroupToPGroupMetadataRepository;
        private readonly ISerologyToAllelesMetadataRepository serologyToAllelesMetadataRepository;

        public HlaMetadataDictionaryFactory(
            IPersistentCacheProvider cacheProvider,

            //For Dictionary
            IRecreateHlaMetadataService recreateMetadataService,
            IHlaConverter hlaConverter,
            IHlaValidator hlaValidator,
            IHlaMatchingMetadataService hlaMatchingMetadataService,
            ILocusHlaMatchingMetadataService locusHlaMatchingMetadataService,
            IHlaScoringMetadataService hlaScoringMetadataService,
            IDpb1TceGroupMetadataService dpb1TceGroupMetadataService,
            IGGroupToPGroupMetadataService gGroupToPGroupMetadataService,
            ISmallGGroupToPGroupMetadataService smallGGroupToPGroupMetadataService,
            ISerologyToAllelesMetadataService serologyToAllelesMetadataService,
            IAlleleNamesMetadataService alleleNamesMetadataService,
            IHlaMetadataGenerationOrchestrator hlaMetadataGenerationOrchestrator,
            IWmdaHlaNomenclatureVersionAccessor wmdaHlaNomenclatureVersionAccessor,
            ILogger logger,

            //For CacheControl
            IAlleleNamesMetadataRepository alleleNamesRepository,
            IHlaMatchingMetadataRepository matchingMetadataRepository,
            IHlaScoringMetadataRepository scoringMetadataRepository,
            IDpb1TceGroupsMetadataRepository dpb1TceGroupsMetadataRepository,
            IGGroupToPGroupMetadataRepository gGroupToPGroupMetadataRepository,
            IHlaNameToSmallGGroupLookupRepository hlaNameToSmallGGroupLookupRepository,
            ISmallGGroupToPGroupMetadataRepository smallGGroupToPGroupMetadataRepository,
            ISerologyToAllelesMetadataRepository serologyToAllelesMetadataRepository)
        {
            this.cache = cacheProvider.Cache;

            //For Dictionary
            this.recreateMetadataService = recreateMetadataService;
            this.hlaConverter = hlaConverter;
            this.hlaValidator = hlaValidator;
            this.hlaMatchingMetadataService = hlaMatchingMetadataService;
            this.locusHlaMatchingMetadataService = locusHlaMatchingMetadataService;
            this.hlaScoringMetadataService = hlaScoringMetadataService;
            this.dpb1TceGroupMetadataService = dpb1TceGroupMetadataService;
            this.gGroupToPGroupMetadataService = gGroupToPGroupMetadataService;
            this.smallGGroupToPGroupMetadataService = smallGGroupToPGroupMetadataService;
            this.serologyToAllelesMetadataService = serologyToAllelesMetadataService;
            this.alleleNamesMetadataService = alleleNamesMetadataService;
            this.hlaMetadataGenerationOrchestrator = hlaMetadataGenerationOrchestrator;
            this.wmdaHlaNomenclatureVersionAccessor = wmdaHlaNomenclatureVersionAccessor;
            this.logger = logger;

            //For CacheControl
            this.alleleNamesRepository = alleleNamesRepository;
            this.matchingMetadataRepository = matchingMetadataRepository;
            this.scoringMetadataRepository = scoringMetadataRepository;
            this.dpb1TceGroupsMetadataRepository = dpb1TceGroupsMetadataRepository;
            this.gGroupToPGroupMetadataRepository = gGroupToPGroupMetadataRepository;
            this.hlaNameToSmallGGroupLookupRepository = hlaNameToSmallGGroupLookupRepository;
            this.smallGGroupToPGroupMetadataRepository = smallGGroupToPGroupMetadataRepository;
            this.serologyToAllelesMetadataRepository = serologyToAllelesMetadataRepository;
        }

        /// <summary>
        /// Dedicated Tuple since we want to store the Dictionary and its CacheControl together.
        /// </summary>
        private class CacheObject
        {
            public IHlaMetadataDictionary Dictionary { get; set; }
            public IHlaMetadataCacheControl CacheControl { get; set; }
        }

        private static string CacheKey(string activeHlaNomenclatureVersion) => $"hlaMetadataDictionary-version:{activeHlaNomenclatureVersion}";

        public IHlaMetadataDictionary BuildDictionary(string activeHlaNomenclatureVersion)
        {
            var key = CacheKey(activeHlaNomenclatureVersion);
            var cachedTuple = cache.GetOrAdd(key, () => BuildTuple(activeHlaNomenclatureVersion));
            return cachedTuple.Dictionary;
        }

        public IHlaMetadataCacheControl BuildCacheControl(string activeHlaNomenclatureVersion)
        {
            var key = CacheKey(activeHlaNomenclatureVersion);
            var cachedTuple = cache.GetOrAdd(key, () => BuildTuple(activeHlaNomenclatureVersion));
            return cachedTuple.CacheControl;
        }

        private CacheObject BuildTuple(string activeHlaNomenclatureVersion)
        {
            if (activeHlaNomenclatureVersion == null)
            {
                throw new Exception("Cannot create a HLA Metadata Dictionary without a nomenclature version.");
            }

            return new CacheObject
            {
                Dictionary = BuildUncachedDictionary(activeHlaNomenclatureVersion),
                CacheControl = BuildUncachedDictionaryCacheControl(activeHlaNomenclatureVersion)
            };
        }

        private IHlaMetadataDictionary BuildUncachedDictionary(string activeHlaNomenclatureVersion)
        {
            return new HlaMetadataDictionary(
                activeHlaNomenclatureVersion,
                recreateMetadataService,
                hlaConverter,
                hlaValidator,
                hlaMatchingMetadataService,
                locusHlaMatchingMetadataService,
                hlaScoringMetadataService,
                dpb1TceGroupMetadataService,
                gGroupToPGroupMetadataService,
                smallGGroupToPGroupMetadataService,
                serologyToAllelesMetadataService,
                alleleNamesMetadataService,
                hlaMetadataGenerationOrchestrator,
                wmdaHlaNomenclatureVersionAccessor,
                logger);
        }

        private IHlaMetadataCacheControl BuildUncachedDictionaryCacheControl(string activeHlaNomenclatureVersion)
        {
            return new HlaMetadataCacheControl(
                activeHlaNomenclatureVersion,
                alleleNamesRepository,
                matchingMetadataRepository,
                scoringMetadataRepository,
                dpb1TceGroupsMetadataRepository,
                gGroupToPGroupMetadataRepository,
                hlaNameToSmallGGroupLookupRepository,
                smallGGroupToPGroupMetadataRepository,
                serologyToAllelesMetadataRepository);
        }
    }
}