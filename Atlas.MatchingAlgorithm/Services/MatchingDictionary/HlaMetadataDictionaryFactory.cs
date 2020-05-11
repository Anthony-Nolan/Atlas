using Atlas.HlaMetadataDictionary.Repositories;
using Atlas.HlaMetadataDictionary.Services;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using LazyCache;

    //QQ Migrate to HlaMdDictionary.
namespace Atlas.MatchingAlgorithm.Services.MatchingDictionary
{
    public struct HlaMetadataConfiguration
    {
        public string ActiveWmdaVersion { get; set; }
        public string WmdaSourceUrl { get; set; }
        public string DbConnectionString { get; set; }

        internal string CacheKey => $"hlaMetadataDictionary-version:{ActiveWmdaVersion}-url:{WmdaSourceUrl}-db:{DbConnectionString}";
    }

    public interface IHlaMetadataDictionaryFactory
    {
        /// <summary>
        /// Returns an appropriate Dictionary or CacheControl, given the config settings.
        /// Calling code is responsible for ensuring that e.g. config.ActiveWmdaVersion is updating per-request.
        /// </summary>
        /// <remarks>
        /// Returns from a cross-request PersistentCache if the Config values are familiar.
        /// If not, builds a new appropriate Dictionary and CacheControl, stores them and returns them.
        /// </remarks>
        IHlaMetadataDictionary BuildDictionary(HlaMetadataConfiguration config);

        /// <inheritdoc cref="BuildDictionary"/>
        IHlaMetadataCacheControl BuildCacheControl(HlaMetadataConfiguration config);
    }

    /// <summary>
    /// Responsible for creating (and handling appropriate caching) of the entry points to the HlaMetadataDictionary library.
    /// </summary>
    /// <remarks>
    /// We expect the Factory itself to be Transient, but it gets a reference to a singleton Persistent Cache of the Dictionaries that it has made previously.
    /// It needs to be Transient so that it is using a new copy of the dependencies each time it tries to create a fresh object.
    /// </remarks>
    public class HlaMetadataDictionaryFactory : IHlaMetadataDictionaryFactory
    {
        private readonly IAppCache cache;
        /*//QQ Defined and register our own IPersistentCache:

                // The default IAppCache registration should be a singleton, to avoid re-caching large collections e.g. Matching Dictionary and Alleles each request
                // Persistent has been picked as the default for ease of injection into the MatchingDictionary, which will not be able to access any wrappers defined in the core project
                services.AddSingleton<IAppCache, CachingService>(sp =>
                {
                    var oneDay = 60*60*24;
                    return new CachingService(new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions())))
                    {
                        DefaultCachePolicy = new CacheDefaults {DefaultCacheDurationSeconds = oneDay}
                    };
                });

         */

        //Dependencies for building new dictionaries. //QQ Module must register these as Re-used within a request, but not between requests. // Would prefer to migrate to Autofac, and declare these as Func<T> so that we have control over initialisation time :(
        //For Dictionary
        private readonly IRecreateHlaMetadataService recreateMetadataService;
        private readonly IAlleleNamesLookupService alleleNamesLookupService;
        private readonly IHlaMatchingLookupService hlaMatchingLookupService;
        private readonly ILocusHlaMatchingLookupService locusHlaMatchingLookupService;
        private readonly IHlaScoringLookupService hlaScoringLookupService;
        private readonly IHlaLookupResultsService hlaLookupResultsService;
        private readonly IDpb1TceGroupLookupService dpb1TceGroupLookupService;
        private readonly IWmdaHlaVersionProvider wmdaHlaVersionProvider;

        //For CacheControl
        private readonly IAlleleNamesLookupRepository alleleNamesRepository;
        private readonly IHlaMatchingLookupRepository matchingLookupRepository;
        private readonly IHlaScoringLookupRepository scoringLookupRepository;
        private readonly IDpb1TceGroupsLookupRepository dpb1TceGroupsLookupRepository;


        public HlaMetadataDictionaryFactory(
            IAppCache cache,

            //For Dictionary
            IRecreateHlaMetadataService recreateMetadataService,
            IAlleleNamesLookupService alleleNamesLookupService,
            IHlaMatchingLookupService hlaMatchingLookupService,
            ILocusHlaMatchingLookupService locusHlaMatchingLookupService,
            IHlaScoringLookupService hlaScoringLookupService,
            IHlaLookupResultsService hlaLookupResultsService,
            IDpb1TceGroupLookupService dpb1TceGroupLookupService,
            IWmdaHlaVersionProvider wmdaHlaVersionProvider,

            //For CacheControl
            IAlleleNamesLookupRepository alleleNamesRepository,
            IHlaMatchingLookupRepository matchingLookupRepository,
            IHlaScoringLookupRepository scoringLookupRepository,
            IDpb1TceGroupsLookupRepository dpb1TceGroupsLookupRepository
            )
        {
            this.cache = cache;

            //For Dictionary
            this.recreateMetadataService = recreateMetadataService;
            this.alleleNamesLookupService = alleleNamesLookupService;
            this.hlaMatchingLookupService = hlaMatchingLookupService;
            this.locusHlaMatchingLookupService = locusHlaMatchingLookupService;
            this.hlaScoringLookupService = hlaScoringLookupService;
            this.hlaLookupResultsService = hlaLookupResultsService;
            this.dpb1TceGroupLookupService = dpb1TceGroupLookupService;
            this.wmdaHlaVersionProvider = wmdaHlaVersionProvider;

            //For CacheControl
            this.alleleNamesRepository = alleleNamesRepository;
            this.matchingLookupRepository = matchingLookupRepository;
            this.scoringLookupRepository = scoringLookupRepository;
            this.dpb1TceGroupsLookupRepository = dpb1TceGroupsLookupRepository;

        }

        /// <summary>
        /// Dedicated Tuple since we want to store the Dictionary and its CacheControl together.
        /// </summary>
        private class CacheObject
        {
            public IHlaMetadataDictionary Dictionary { get; set; }
            public IHlaMetadataCacheControl CacheControl { get; set; }
        }

        public IHlaMetadataDictionary BuildDictionary(HlaMetadataConfiguration config)
        {
            return GetFromCache_Or_CreateAndAddToCache(config).Dictionary;
        }

        public IHlaMetadataCacheControl BuildCacheControl(HlaMetadataConfiguration config)
        {
            return GetFromCache_Or_CreateAndAddToCache(config).CacheControl;
        }

        private CacheObject GetFromCache_Or_CreateAndAddToCache(HlaMetadataConfiguration config)
        {
            var cachedTuple = cache.GetOrAdd(config.CacheKey, () => BuildTuple(config));
            return cachedTuple;
        }

        private CacheObject BuildTuple(HlaMetadataConfiguration config)
        {
            return new CacheObject { 
                Dictionary = BuildUncachedDictionary(config),
                CacheControl = BuildUncachedDictionaryCacheControl(config)
            };
        }

        private IHlaMetadataDictionary BuildUncachedDictionary(HlaMetadataConfiguration config)
        {
            return new HlaMetadataDictionary(
                config,
                recreateMetadataService,
                alleleNamesLookupService,
                hlaMatchingLookupService,
                locusHlaMatchingLookupService,
                hlaScoringLookupService,
                hlaLookupResultsService,
                dpb1TceGroupLookupService,
                wmdaHlaVersionProvider);
        }

        private IHlaMetadataCacheControl BuildUncachedDictionaryCacheControl(HlaMetadataConfiguration config)
        {
            return new HlaMetadataCacheControl(
                config,
                alleleNamesRepository,
                matchingLookupRepository,
                scoringLookupRepository,
                dpb1TceGroupsLookupRepository);
        }
    }
}