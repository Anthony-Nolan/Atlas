using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Caching;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.ExternalInterface.Exceptions;
using Atlas.HlaMetadataDictionary.InternalModels.Metadata;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;
using Atlas.HlaMetadataDictionary.Services.DataRetrieval;
using AwesomeAssertions;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.UnitTests.ExternalInterface
{
    /// <summary>
    /// Driven through a REAL <see cref="AlleleNamesMetadataService"/> against a REAL cache wherever possible, rather
    /// than against hand-written cache keys. The risk this design carries is that a key format drifts and stops being
    /// recognised - which fails silently, as an entry that is never evicted - so the tests that matter are the ones
    /// where production code mints the key.
    /// </summary>
    [TestFixture]
    internal class HlaMetadataCacheInvalidatorTests
    {
        private const string RecreatedVersion = "3650";
        private const string OtherVersion = "3660";
        private const string AlleleName = "A*01:01";

        private IPersistentCacheProvider cacheProvider;
        private IAlleleNamesMetadataRepository repository;
        private AlleleNamesMetadataService metadataService;
        private IHlaMetadataCacheInvalidator invalidator;

        [SetUp]
        public void SetUp()
        {
            cacheProvider = AppCacheBuilder.NewPersistentCacheProvider();

            repository = Substitute.For<IAlleleNamesMetadataRepository>();
            repository.GetAlleleNameIfExists(default, default, default)
                .ReturnsForAnyArgs(new AlleleNameMetadata("A*", default, default));

            var hlaCategorisationService = Substitute.For<IHlaCategorisationService>();
            hlaCategorisationService.GetHlaTypingCategory(default).ReturnsForAnyArgs(HlaTypingCategory.Allele);

            metadataService = new AlleleNamesMetadataService(repository, hlaCategorisationService, cacheProvider);
            invalidator = new HlaMetadataCacheInvalidator(cacheProvider, Substitute.For<IAtlasLogger>());
        }

        /// <summary>
        /// The whole point: after invalidation the next lookup must go back to storage, not be served from memory.
        /// </summary>
        [Test]
        public async Task InvalidateCaches_MakesTheNextLookupReReadFromStorage()
        {
            await metadataService.GetCurrentAlleleNames(Locus.A, AlleleName, RecreatedVersion);
            await metadataService.GetCurrentAlleleNames(Locus.A, AlleleName, RecreatedVersion);
            await repository.Received(1).GetAlleleNameIfExists(Locus.A, AlleleName, RecreatedVersion);

            invalidator.InvalidateCaches(RecreatedVersion);
            await metadataService.GetCurrentAlleleNames(Locus.A, AlleleName, RecreatedVersion);

            await repository.Received(2).GetAlleleNameIfExists(Locus.A, AlleleName, RecreatedVersion);
        }

        /// <summary>
        /// The case this scoping exists for. A data refresh recreates the dictionary at a NEW version as its first
        /// stage, while every running matching app carries on serving the PREVIOUS version for the hours until that
        /// refresh completes. Evicting the version it is actively serving would cold-start it on every refresh.
        /// </summary>
        [Test]
        public async Task InvalidateCaches_LeavesOtherNomenclatureVersionsIntact()
        {
            await metadataService.GetCurrentAlleleNames(Locus.A, AlleleName, OtherVersion);
            await repository.Received(1).GetAlleleNameIfExists(Locus.A, AlleleName, OtherVersion);

            invalidator.InvalidateCaches(RecreatedVersion);
            await metadataService.GetCurrentAlleleNames(Locus.A, AlleleName, OtherVersion);

            await repository.Received(1).GetAlleleNameIfExists(Locus.A, AlleleName, OtherVersion);
        }

        /// <summary>
        /// The entry that actually caused the defect. A name with no row is cached as a not-found OUTCOME under a key of
        /// its own - the lookup then throws on every subsequent call WITHOUT going back to storage, which is how a
        /// donor carrying a newly-added serology code kept being skipped after the dictionary had been fixed.
        /// Evicting only the whole-table entries would leave that outcome in place.
        /// </summary>
        [Test]
        public async Task InvalidateCaches_EvictsCachedNotFoundOutcomes()
        {
            repository.GetAlleleNameIfExists(default, default, default).ReturnsNullForAnyArgs();

            await LookUpIgnoringNotFound();
            await LookUpIgnoringNotFound();
            await repository.Received(1).GetAlleleNameIfExists(Locus.A, AlleleName, RecreatedVersion);

            invalidator.InvalidateCaches(RecreatedVersion);
            await LookUpIgnoringNotFound();

            await repository.Received(2).GetAlleleNameIfExists(Locus.A, AlleleName, RecreatedVersion);
        }

        /// <summary>A not-found lookup throws; here the throwing is expected and the repository call is the subject.</summary>
        private async Task LookUpIgnoringNotFound()
        {
            try
            {
                await metadataService.GetCurrentAlleleNames(Locus.A, AlleleName, RecreatedVersion);
            }
            catch (HlaMetadataDictionaryException)
            {
            }
        }

        /// <summary>
        /// Data that isn't derived from the dictionary shares this cache. Match Prediction's haplotype frequency sets
        /// are the expensive case - they repopulate through a slow background warm.
        /// </summary>
        [Test]
        public void InvalidateCaches_LeavesDataNotDerivedFromTheDictionaryIntact()
        {
            cacheProvider.Cache.GetOrAdd("haplotype-frequency-set:123", _ => "expensive-to-rebuild");

            invalidator.InvalidateCaches(RecreatedVersion);

            cacheProvider.Cache.Get<string>("haplotype-frequency-set:123").Should().Be("expensive-to-rebuild");
        }

        [Test]
        public void InvalidateCaches_ForAVersionNothingHasCached_DoesNotThrow()
        {
            invalidator.Invoking(i => i.InvalidateCaches("a-version-never-looked-up")).Should().NotThrow();
        }

        [Test]
        public async Task InvalidateCaches_LeavesCacheUsable()
        {
            invalidator.InvalidateCaches(RecreatedVersion);

            await metadataService.GetCurrentAlleleNames(Locus.A, AlleleName, RecreatedVersion);
            await metadataService.GetCurrentAlleleNames(Locus.A, AlleleName, RecreatedVersion);

            await repository.Received(1).GetAlleleNameIfExists(Locus.A, AlleleName, RecreatedVersion);
        }
    }
}
