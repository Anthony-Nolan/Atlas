using System.Threading.Tasks;
using Atlas.Common.Caching;
using Atlas.Common.GeneticData;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.HlaMetadataDictionary.ExternalInterface.Exceptions;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;
using Atlas.HlaMetadataDictionary.Services.DataRetrieval;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs;
using FluentAssertions;
using LazyCache;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.IntegrationTests.Tests
{
    [TestFixture]
    public class GGroupToPGroupMetadataServiceTests
    {
        private const string CacheKey = nameof(GGroupToPGroupMetadataRepository);
        private const string HlaVersion = FileBackedHlaMetadataRepositoryBaseReader.OlderTestHlaVersion;

        private IGGroupToPGroupMetadataService metadataService;
        private IAppCache appCache;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                metadataService = DependencyInjection.DependencyInjection.Provider.GetService<IGGroupToPGroupMetadataService>();
                appCache = DependencyInjection.DependencyInjection.Provider.GetService<IPersistentCacheProvider>().Cache;
            });
        }

        [TearDown]
        public void TearDown()
        {
            // clear MAC allele mappings between tests
            appCache.Remove(CacheKey);
        }

        [TestCase(Locus.A, "01:01:01G", "01:01P")]
        [TestCase(Locus.B, "08:01:01G", "08:01P")]
        [TestCase(Locus.C, "07:01:01G", "07:01P")]
        [TestCase(Locus.Dqb1, "02:01:01G", "02:01P")]
        [TestCase(Locus.Drb1, "03:01:01G", "03:01P")]
        public async Task GetSinglePGroupForGGroup_WithMatchingPGroup_ReturnsPGroup(Locus locus, string gGroup, string expectedPGroup)
        {
            var pGroup = await metadataService.ConvertGGroupToPGroup(locus, gGroup, HlaVersion);

            pGroup.Should().Be(expectedPGroup);
        }

        [TestCase(Locus.A, "01:11N")]
        [TestCase(Locus.B, "35:216N")]
        [TestCase(Locus.C, "07:491:01N")]
        [TestCase(Locus.Dqb1, "02:20N")]
        [TestCase(Locus.Drb1, "08:78N")]
        public async Task GetSinglePGroupForGGroup_WithNoMatchingPGroup_ReturnsNull(Locus locus, string gGroup)
        {
            var pGroup = await metadataService.ConvertGGroupToPGroup(locus, gGroup, HlaVersion);

            pGroup.Should().Be(null);
        }

        [Test]
        public void GetSinglePGroupForGGroup_ForInvalidGGroup_ThrowsException()
        {
            metadataService.Invoking(async service =>
                 await service.ConvertGGroupToPGroup(Locus.A, "not-a-valid-g-group", HlaVersion)
                ).Should().Throw<HlaMetadataDictionaryException>();
        }
    }
}