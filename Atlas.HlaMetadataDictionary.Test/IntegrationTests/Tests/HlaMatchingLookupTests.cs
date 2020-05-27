using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.Caching;
using Atlas.Common.GeneticData;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.HlaMetadataDictionary.Services.DataRetrieval;
using Atlas.MultipleAlleleCodeDictionary.HlaService;
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
    public class HlaMatchingLookupLookupTests
    {
        private const Locus DefaultLocus = Locus.A;
        private const string CacheKey = "NmdpCodeLookup_A";

        private IHlaMatchingLookupService lookupService;
        private IHlaServiceClient hlaServiceClient;
        private IAppCache appCache;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                lookupService = DependencyInjection.DependencyInjection.Provider.GetService<IHlaMatchingLookupService>();
                hlaServiceClient = DependencyInjection.DependencyInjection.Provider.GetService<IHlaServiceClient>();
                appCache = DependencyInjection.DependencyInjection.Provider.GetService<IPersistentCacheProvider>().Cache;
            });
        }

        [SetUp]
        public void SetUp()
        {
            hlaServiceClient
                .GetAllelesForDefinedNmdpCode(DefaultLocus, Arg.Any<string>())
                .Returns(new List<string>());
 
            // clear NMDP code allele mappings between tests
            appCache.Remove(CacheKey);
        }

        [Test]
        public async Task GetHlaLookupResult_WhenNmdpCode_ReturnsMatchingHlaForAllAlleles()
        {
            // each allele maps to a P group of the same name
            const string firstAllele = "01:133";
            const string secondAllele = "01:158";

            // NMDP code value does not matter, but does need to conform to the expected pattern
            const string nmdpCode = "99:CODE";
            hlaServiceClient
                .GetAllelesForDefinedNmdpCode(DefaultLocus, nmdpCode)
                .Returns(new List<string> { firstAllele, secondAllele });

            var result = await lookupService.GetHlaLookupResult(DefaultLocus, nmdpCode, null);

            result.MatchingPGroups.Should().BeEquivalentTo(new[] { firstAllele, secondAllele });
        }

        [Test]
        public async Task GetHlaLookupResult_WhenAlleleStringOfNames_ReturnsMatchingHlaForAllAlleles()
        {
            // each allele maps to a P group of the same name
            const string firstAllele = "01:133";
            const string secondAllele = "01:158";
            const string alleleString = firstAllele + "/" + secondAllele;

            var result = await lookupService.GetHlaLookupResult(DefaultLocus, alleleString, null);

            result.MatchingPGroups.Should().BeEquivalentTo(new[] { firstAllele, secondAllele });
        }

        [Test]
        public async Task GetHlaLookupResult_WhenAlleleStringOfSubtypes_ReturnsMatchingHlaForAllAlleles()
        {
            // each allele maps to a P group of the same name
            const string firstAllele = "01:133";
            const string secondAllele = "01:158";
            const string alleleString = "01:133/158";

            var result = await lookupService.GetHlaLookupResult(DefaultLocus, alleleString, null);

            result.MatchingPGroups.Should().BeEquivalentTo(new[] { firstAllele, secondAllele });
        }
    }
}
