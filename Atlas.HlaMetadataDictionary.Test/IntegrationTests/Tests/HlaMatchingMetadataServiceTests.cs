using System.Threading.Tasks;
using Atlas.Common.Caching;
using Atlas.Common.GeneticData;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Test.SharedTestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Atlas.HlaMetadataDictionary.Services.DataRetrieval;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs;
using FluentAssertions;
using LazyCache;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.IntegrationTests.Tests
{
    /// <summary>
    /// Fixture relies on a file-backed HlaMetadataDictionary - tests may break if underlying data is changed.
    /// </summary>
    [TestFixture]
    public class HlaMatchingMetadataServiceTests
    {
        private const Locus DefaultLocus = Locus.A;
        private const string CacheKey = "NmdpCodeLookup_A";
        private const string HlaVersion = FileBackedHlaMetadataRepositoryBaseReader.OlderTestHlaVersion;

        private IHlaMatchingMetadataService metadataService;
        private IAppCache appCache;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                metadataService = DependencyInjection.DependencyInjection.Provider.GetService<IHlaMatchingMetadataService>();
                appCache = DependencyInjection.DependencyInjection.Provider.GetService<IPersistentCacheProvider>().Cache;
            });
        }

        [TearDown]
        public void TearDown()
        {
            // clear MAC allele mappings between tests
            appCache.Remove(CacheKey);
        }

        [Test]
        public async Task GetHlaMetadata_WhenPGroup_MatchingHlaOnlyContainsSamePGroup()
        {
            const string pGroup = "01:01P";

            var result = await metadataService.GetHlaMetadata(DefaultLocus, pGroup, HlaVersion);

            result.MatchingPGroups.Should().BeEquivalentTo(pGroup);
        }

        [Test]
        public async Task GetHlaMetadata_WhenGGroup_MatchingHlaOnlyContainsExpectedPGroup()
        {
            const string gGroup = "01:01:01G";
            const string expectedPGroup = "01:01P";

            var result = await metadataService.GetHlaMetadata(DefaultLocus, gGroup, HlaVersion);

            result.MatchingPGroups.Should().BeEquivalentTo(expectedPGroup);
        }

        [Test]
        public async Task GetHlaMetadata_WhenMac_ReturnsMatchingHlaForAllAlleles()
        {
            // each allele maps to a P group of the same name
            const string firstAllele = "01:133";
            const string secondAllele = "01:158";

            // MAC value here should be represented in Atlas.MultipleAlleleCodeDictionary.Test.Integration.Resources.Mac.csv
            const string macWithFirstField = "01:XYZ";


            var result = await metadataService.GetHlaMetadata(DefaultLocus, macWithFirstField, HlaVersion);

            result.MatchingPGroups.Should().BeEquivalentTo(firstAllele, secondAllele);
        }

        [Test]
        public async Task GetHlaMetadata_WhenAlleleStringOfNames_ReturnsMatchingHlaForAllAlleles()
        {
            // each allele maps to a P group of the same name
            const string firstAllele = "01:133";
            const string secondAllele = "01:158";
            const string alleleString = firstAllele + "/" + secondAllele;

            var result = await metadataService.GetHlaMetadata(DefaultLocus, alleleString, HlaVersion);

            result.MatchingPGroups.Should().BeEquivalentTo(firstAllele, secondAllele);
        }

        [Test]
        public async Task GetHlaMetadata_WhenAlleleStringOfSubtypes_ReturnsMatchingHlaForAllAlleles()
        {
            // each allele maps to a P group of the same name
            const string firstAllele = "01:133";
            const string secondAllele = "01:158";
            const string alleleString = "01:133/158";

            var result = await metadataService.GetHlaMetadata(DefaultLocus, alleleString, HlaVersion);

            result.MatchingPGroups.Should().BeEquivalentTo(firstAllele, secondAllele);
        }
    }
}