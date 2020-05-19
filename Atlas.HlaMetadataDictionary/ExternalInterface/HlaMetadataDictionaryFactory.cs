using Atlas.Common.Caching;
using Atlas.HlaMetadataDictionary.Repositories.LookupRepositories;
using Atlas.HlaMetadataDictionary.Services;
using Atlas.HlaMetadataDictionary.Services.DataGeneration;
using Atlas.HlaMetadataDictionary.Services.DataRetrieval;
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
    public class HlaMetadataDictionaryFactory : IHlaMetadataDictionaryFactory
    {
        private readonly IAppCache cache;

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


        internal HlaMetadataDictionaryFactory(
            IPersistentCacheProvider cacheProvider,

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
            this.cache = cacheProvider.Cache;

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

        internal string CacheKey(string activeHlaNomenclatureVersion) => $"hlaMetadataDictionary-version:{activeHlaNomenclatureVersion}";

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
            return new CacheObject { 
                Dictionary = BuildUncachedDictionary(activeHlaNomenclatureVersion),
                CacheControl = BuildUncachedDictionaryCacheControl(activeHlaNomenclatureVersion)
            };
        }

        private IHlaMetadataDictionary BuildUncachedDictionary(string activeHlaNomenclatureVersion)
        {
            return new HlaMetadataDictionary(
                activeHlaNomenclatureVersion,
                recreateMetadataService,
                alleleNamesLookupService,
                hlaMatchingLookupService,
                locusHlaMatchingLookupService,
                hlaScoringLookupService,
                hlaLookupResultsService,
                dpb1TceGroupLookupService,
                wmdaHlaVersionProvider);
        }

        private IHlaMetadataCacheControl BuildUncachedDictionaryCacheControl(string activeHlaNomenclatureVersion)
        {
            return new HlaMetadataCacheControl(
                activeHlaNomenclatureVersion,
                alleleNamesRepository,
                matchingLookupRepository,
                scoringLookupRepository,
                dpb1TceGroupsLookupRepository);
        }
    }
}