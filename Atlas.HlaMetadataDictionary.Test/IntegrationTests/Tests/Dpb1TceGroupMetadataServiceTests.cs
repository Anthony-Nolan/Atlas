using System.Threading.Tasks;
using Atlas.Common.Caching;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.HlaMetadataDictionary.Services.DataRetrieval;
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
    public class Dpb1TceGroupMetadataServiceTests
    {
        private const string CacheKey = "NmdpCodeLookup_Dpb1";

        private IDpb1TceGroupMetadataService metadataService;
        private IAppCache appCache;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                metadataService = DependencyInjection.DependencyInjection.Provider.GetService<IDpb1TceGroupMetadataService>();
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
        public async Task GetDpb1TceGroup_WhenPGroup_ReturnsTceGroup()
        {
            const string pGroup = "01:01P";
            const string tceGroup = "3";

            var result = await metadataService.GetDpb1TceGroup(pGroup, null);

            result.Should().Be(tceGroup);
        }

        [Test]
        public async Task GetDpb1TceGroup_WhenGGroup_ReturnsTceGroup()
        {
            const string pGroup = "01:01:01G";
            const string tceGroup = "3";

            var result = await metadataService.GetDpb1TceGroup(pGroup, null);

            result.Should().Be(tceGroup);
        }

        [Test]
        public async Task GetDpb1TceGroup_WhenNmdpCodeMapsToSingleTceGroup_ReturnsTceGroup()
        {
            // both alleles map to same TCE group
            const string tceGroup = "3";

            // MAC value here should be represented in Atlas.MultipleAlleleCodeDictionary.Test.Integration.Resources.Mac.csv
            const string macWithFirstField = "02:ABC";

            var result = await metadataService.GetDpb1TceGroup(macWithFirstField, null);

            result.Should().Be(tceGroup);
        }

        [Test]
        public async Task GetDpb1TceGroup_WhenNmdpCodeMapsToMoreThanOneTceGroup_DoesNotReturnTceGroup()
        {
            // alleles map to different TCE groups
            // MAC value here should be represented in Atlas.MultipleAlleleCodeDictionary.Test.Integration.Resources.Mac.csv
            const string macWithFirstField = "02:DEF";

            var result = await metadataService.GetDpb1TceGroup(macWithFirstField, null);

            result.Should().BeNullOrEmpty();
        }

        [Test]
        public async Task GetDpb1TceGroup_WhenAlleleStringOfNamesMapsToSingleTceGroup_ReturnsTceGroup()
        {
            // both alleles map to same TCE group
            const string firstAllele = "02:01";
            const string secondAllele = "02:02";
            const string tceGroup = "3";
            const string alleleString = firstAllele + "/" + secondAllele;

            var result = await metadataService.GetDpb1TceGroup(alleleString, null);

            result.Should().Be(tceGroup);
        }

        [Test]
        public async Task GetDpb1TceGroup_WhenAlleleStringOfSubtypesMapsToSingleTceGroup_ReturnsTceGroup()
        {
            // both alleles map to same TCE group
            const string tceGroup = "3";
            const string alleleString = "02:01/02";

            var result = await metadataService.GetDpb1TceGroup(alleleString, null);

            result.Should().Be(tceGroup);
        }

        [Test]
        public async Task GetDpb1TceGroup_WhenAlleleStringOfNamesMapsToMultipleTceGroups_DoesNotReturnTceGroup()
        {
            // alleles map to different TCE group
            const string firstAllele = "02:01";
            const string secondAllele = "03:01";
            const string alleleString = firstAllele + "/" + secondAllele;

            var result = await metadataService.GetDpb1TceGroup(alleleString, null);

            result.Should().BeNullOrEmpty();
        }
    }
}