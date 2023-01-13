using System.Threading.Tasks;
using Atlas.Common.Caching;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.HlaMetadataDictionary.InternalModels.Metadata;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;
using Atlas.HlaMetadataDictionary.Services.DataRetrieval;
using LazyCache;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.UnitTests.Services.DataRetrieval.MetadataServices
{
    /// <summary>
    /// <see cref="MetadataServiceBase{T}"/> is abstract; concrete class used to test base functionality.
    /// </summary>
    [TestFixture]
    public class MetadataServiceBaseTests
    {
        private AlleleNamesMetadataService metadataService;
        private IAlleleNamesMetadataRepository repository;
        private IHlaCategorisationService hlaCategorisationService;
        private IAppCache cache;

        [SetUp]
        public void SetUp()
        {
            repository = Substitute.For<IAlleleNamesMetadataRepository>();
            hlaCategorisationService = Substitute.For<IHlaCategorisationService>();

            cache = AppCacheBuilder.NewDefaultCache();
            var cacheProvider = Substitute.For<IPersistentCacheProvider>();
            cacheProvider.Cache.Returns(cache);

            metadataService = new AlleleNamesMetadataService(repository, hlaCategorisationService, cacheProvider);

            repository.GetAlleleNameIfExists(default, default, default)
                .ReturnsForAnyArgs(new AlleleNameMetadata("A*", default, default));

            hlaCategorisationService.GetHlaTypingCategory(default).ReturnsForAnyArgs(HlaTypingCategory.Allele);
        }

        [Test]
        public async Task GetMetadata_CacheDoesNotContainMetadataValue_FetchesMetadataFromRepository()
        {
            const Locus locus = Locus.A;
            const string lookupName = "hla";
            const string version = "version";

            await metadataService.GetCurrentAlleleNames(locus, lookupName, version);

            await repository.Received().GetAlleleNameIfExists(locus, lookupName, version);
        }

        [Test]
        public async Task GetMetadata_MultipleLookupsWithSameParameters_OnlyFetchesMetadataFromRepositoryOnce()
        {
            const Locus locus = Locus.A;
            const string lookupName = "hla";
            const string version = "version";

            await Task.WhenAll(
                metadataService.GetCurrentAlleleNames(locus, lookupName, version),
                metadataService.GetCurrentAlleleNames(locus, lookupName, version),
                metadataService.GetCurrentAlleleNames(locus, lookupName, version),
                metadataService.GetCurrentAlleleNames(locus, lookupName, version)
            );

            await repository.Received(1).GetAlleleNameIfExists(locus, lookupName, version);
        }

        [Test]
        public async Task GetMetadata_MultipleLookupsWithSameHlaButDifferentVersions_OnlyFetchesMetadataFromRepositoryOncePerVersion()
        {
            const Locus locus = Locus.A;
            const string lookupName = "hla";
            const string versionOne = "version-1";
            const string versionTwo = "version-2";

            await Task.WhenAll(
                metadataService.GetCurrentAlleleNames(locus, lookupName, versionOne),
                metadataService.GetCurrentAlleleNames(locus, lookupName, versionOne),
                metadataService.GetCurrentAlleleNames(locus, lookupName, versionTwo),
                metadataService.GetCurrentAlleleNames(locus, lookupName, versionTwo)
            );

            await repository.Received(1).GetAlleleNameIfExists(locus, lookupName, versionOne);
            await repository.Received(1).GetAlleleNameIfExists(locus, lookupName, versionTwo);

        }
    }
}
