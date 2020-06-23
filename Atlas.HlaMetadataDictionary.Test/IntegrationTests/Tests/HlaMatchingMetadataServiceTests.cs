using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.Caching;
using Atlas.Common.GeneticData;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.HlaMetadataDictionary.Services.DataRetrieval;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface;
using FluentAssertions;
using LazyCache;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
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

        private IHlaMatchingMetadataService metadataService;
        private IAppCache appCache;
        private IMacDictionary macDictionary;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                metadataService = DependencyInjection.DependencyInjection.Provider.GetService<IHlaMatchingMetadataService>();
                appCache = DependencyInjection.DependencyInjection.Provider.GetService<IPersistentCacheProvider>().Cache;
                macDictionary = DependencyInjection.DependencyInjection.Provider.GetService<IMacDictionary>();
            });
        }

        [SetUp]
        public void SetUp()
        {
            // clear MAC allele mappings between tests
            appCache.Remove(CacheKey);
        }

        [Test]
        public async Task GetHlaMetadata_WhenNmdpCode_ReturnsMatchingHlaForAllAlleles()
        {
            // each allele maps to a P group of the same name
            const string firstAllele = "01:133";
            const string secondAllele = "01:158";
            
            const string macWithFirstField = "01:XYZ";

            var result = await metadataService.GetHlaMetadata(DefaultLocus, macWithFirstField, null);

            result.MatchingPGroups.Should().BeEquivalentTo(new[] { firstAllele, secondAllele });
        }

        [Test]
        public async Task GetHlaMetadata_WhenAlleleStringOfNames_ReturnsMatchingHlaForAllAlleles()
        {
            // each allele maps to a P group of the same name
            const string firstAllele = "01:133";
            const string secondAllele = "01:158";
            const string alleleString = firstAllele + "/" + secondAllele;

            var result = await metadataService.GetHlaMetadata(DefaultLocus, alleleString, null);

            result.MatchingPGroups.Should().BeEquivalentTo(new[] { firstAllele, secondAllele });
        }

        [Test]
        public async Task GetHlaMetadata_WhenAlleleStringOfSubtypes_ReturnsMatchingHlaForAllAlleles()
        {
            // each allele maps to a P group of the same name
            const string firstAllele = "01:133";
            const string secondAllele = "01:158";
            const string alleleString = "01:133/158";

            var result = await metadataService.GetHlaMetadata(DefaultLocus, alleleString, null);

            result.MatchingPGroups.Should().BeEquivalentTo(new[] { firstAllele, secondAllele });
        }
    }
}
