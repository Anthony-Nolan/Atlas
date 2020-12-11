using System.Threading.Tasks;
using Atlas.Common.Caching;
using Atlas.Common.GeneticData;
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
    public class SmallGGroupToPGroupMetadataServiceTests
    {
        private const string CacheKey = nameof(SmallGGroupToPGroupMetadataRepository);
        private const string HlaVersion = FileBackedHlaMetadataRepositoryBaseReader.OlderTestHlaVersion;

        private ISmallGGroupToPGroupMetadataService metadataService;
        private IAppCache appCache;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                metadataService = DependencyInjection.DependencyInjection.Provider.GetService<ISmallGGroupToPGroupMetadataService>();
                appCache = DependencyInjection.DependencyInjection.Provider.GetService<IPersistentCacheProvider>().Cache;
            });
        }

        [TearDown]
        public void TearDown()
        {
            appCache.Remove(CacheKey);
        }

        [TestCase(Locus.A, "01:01g", "01:01P")]
        [TestCase(Locus.B, "08:01g", "08:01P")]
        [TestCase(Locus.C, "07:01g", "07:01P")]
        [TestCase(Locus.Dqb1, "02:01g", "02:01P")]
        [TestCase(Locus.Drb1, "03:01g", "03:01P")]
        public async Task GetSinglePGroupForSmallGGroup_WithMatchingPGroup_ReturnsPGroup(Locus locus, string smallGGroup, string expectedPGroup)
        {
            var pGroup = await metadataService.ConvertSmallGGroupToPGroup(locus, smallGGroup, HlaVersion);

            pGroup.Should().Be(expectedPGroup);
        }

        [TestCase(Locus.A, "01:52")]
        [TestCase(Locus.B, "35:173")]
        [TestCase(Locus.C, "07:491")]
        [TestCase(Locus.Dqb1, "02:20N")]
        [TestCase(Locus.Drb1, "08:78N")]
        public async Task GetSinglePGroupForSmallGGroup_WithNoMatchingPGroup_ReturnsNull(Locus locus, string smallGGroup)
        {
            var pGroup = await metadataService.ConvertSmallGGroupToPGroup(locus, smallGGroup, HlaVersion);

            pGroup.Should().Be(null);
        }

        [Test]
        public void GetSinglePGroupForSmallGGroup_ForInvalidSmallGGroup_ThrowsException()
        {
            metadataService.Invoking(async service =>
                 await service.ConvertSmallGGroupToPGroup(Locus.A, "not-a-valid-small-g-group", HlaVersion)
                ).Should().Throw<HlaMetadataDictionaryException>();
        }
    }
}