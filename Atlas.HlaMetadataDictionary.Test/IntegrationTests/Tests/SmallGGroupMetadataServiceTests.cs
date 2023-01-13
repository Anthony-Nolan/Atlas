using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Caching;
using Atlas.Common.GeneticData;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.HlaMetadataDictionary.Services.DataRetrieval;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs;
using FluentAssertions;
using LazyCache;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.IntegrationTests.Tests
{
    /// <summary>
    /// Fixture relies on a file-backed HlaMetadataDictionary - tests may break if underlying data is changed.
    /// </summary>
    [TestFixture]
    public class SmallGGroupMetadataServiceTests
    {
        private const Locus DefaultLocus = Locus.A;
        private const string CacheKey = "NmdpCodeLookup_A";
        private const string HlaVersion = FileBackedHlaMetadataRepositoryBaseReader.OlderTestHlaVersion;

        private ISmallGGroupMetadataService metadataService;
        private IAppCache appCache;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                metadataService = DependencyInjection.DependencyInjection.Provider.GetService<ISmallGGroupMetadataService>();
                appCache = DependencyInjection.DependencyInjection.Provider.GetService<IPersistentCacheProvider>().Cache;
            });
        }
        
        [TearDown]
        public void TearDown()
        {
            // clear MAC allele mappings between tests
            appCache.Remove(CacheKey);
        }

        [TestCase("01:03:01:01", "01:03", Description = "Allele with two-field small g group")]
        [TestCase("11:263", "11:01g", Description = "Allele with g-suffix small g group")]
        [TestCase("24:02:03Q", "24:02g", Description = "Allele with expression letter")]
        [TestCase("01:01:01:02N", "01:01g", Description = "Null allele that does map to P group")]
        [TestCase("01:52:01N", "01:52", Description = "Null allele that doesn't map to P group")]
        public async Task GetSmallGGroups_WhenSingleAllele_ReturnsSmallGGroup(string allele, string expectedSmallGroup)
        {
            var result = await metadataService.GetSmallGGroups(DefaultLocus, allele, HlaVersion);

            result.Single().Should().Be(expectedSmallGroup);
        }

        [Test]
        public async Task GetSmallGGroups_WhenPGroup_ReturnsSmallGGroup()
        {
            const string pGroup = "01:01P";
            const string smallGGroup = "01:01g";

            var result = await metadataService.GetSmallGGroups(DefaultLocus, pGroup, HlaVersion);

            result.Single().Should().Be(smallGGroup);
        }

        [Test]
        public async Task GetSmallGGroups_WhenSerology_ReturnsSmallGGroup()
        {
            const string serology = "1";
            const string smallGGroup = "01:01g";

            var result = (await metadataService.GetSmallGGroups(DefaultLocus, serology, HlaVersion)).ToList();

            result.Should().Contain(smallGGroup);
            result.Count().Should().Be(213);
        }

        [Test]
        public async Task GetSmallGGroups_WhenGGroup_ReturnsSmallGGroup()
        {
            const string pGroup = "01:01:01G";
            const string smallGGroup = "01:01g";

            var result = await metadataService.GetSmallGGroups(DefaultLocus, pGroup, HlaVersion);

            result.Single().Should().Be(smallGGroup);
        }

        [Test]
        public async Task GetSmallGGroups_WhenNmdpCodeMapsToSingleSmallGGroup_ReturnsSmallGGroup()
        {
            // MAC value here should be represented in Atlas.MultipleAlleleCodeDictionary.Test.Integration\Repositories\LargeMacDictionary.csv
            const string macWithFirstField = "02:FDYX";
            const string smallGGroup = "02:05g";

            var result = await metadataService.GetSmallGGroups(DefaultLocus, macWithFirstField, HlaVersion);

            result.Single().Should().Be(smallGGroup);
        }

        [Test]
        public async Task GetSmallGGroups_WhenNmdpCodeMapsToMoreThanOneSmallGGroup_ReturnsAllSmallGGroups()
        {
            // MAC value here should be represented in Atlas.MultipleAlleleCodeDictionary.Test.Integration\Repositories\LargeMacDictionary.csv
            const string macWithFirstField = "02:DEF";
            var smallGGroups = new[] { "02:01g", "03:01g" };

            var result = await metadataService.GetSmallGGroups(DefaultLocus, macWithFirstField, HlaVersion);

            result.Should().BeEquivalentTo(smallGGroups);
        }

        [Test]
        public async Task GetSmallGGroups_WhenAlleleStringOfNamesMapsToSingleSmallGGroup_ReturnsSmallGGroup()
        {
            const string firstAllele = "02:685";
            const string secondAllele = "02:686";
            const string alleleString = firstAllele + "/" + secondAllele;
            const string smallGGroup = "02:01g";

            var result = await metadataService.GetSmallGGroups(DefaultLocus, alleleString, HlaVersion);

            result.Single().Should().Be(smallGGroup);
        }

        [Test]
        public async Task GetSmallGGroups_WhenAlleleStringOfSubtypesMapsToSingleSmallGGroup_ReturnsSmallGGroup()
        {
            const string alleleString = "02:685/686";
            const string smallGGroup = "02:01g";

            var result = await metadataService.GetSmallGGroups(DefaultLocus, alleleString, HlaVersion);

            result.SingleOrDefault().Should().Be(smallGGroup);
        }

        [Test]
        public async Task GetSmallGGroups_WhenAlleleStringOfNamesMapsToMultipleSmallGGroups_ReturnsAllSmallGGroups()
        {
            const string firstAllele = "02:01";
            const string secondAllele = "03:01";
            const string alleleString = firstAllele + "/" + secondAllele;
            var smallGGroups = new[] { "02:01g", "03:01g" };

            var result = await metadataService.GetSmallGGroups(DefaultLocus, alleleString, HlaVersion);

            result.Should().BeEquivalentTo(smallGGroups);
        }
    }
}