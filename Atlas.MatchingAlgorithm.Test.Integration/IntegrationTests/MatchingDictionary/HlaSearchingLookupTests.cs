using LazyCache;
using Microsoft.Extensions.DependencyInjection;
using Atlas.HLAService.Client;
using Atlas.HlaMetadataDictionary.Services;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using Atlas.HlaMetadataDictionary.Exceptions;
using Atlas.Common.Caching;
using Atlas.Common.GeneticData;
using Locus = Atlas.Common.GeneticData.Locus;

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests.HlaMetadataDictionary
{
    /// <summary>
    /// Fixture testing the base functionality of HlaSearchingLookupService via an arbitrarily chosen base class.
    /// Fixture relies on a file-backed HlaMetadataDictionary - tests may break if underlying data is changed.
    /// </summary>
    [TestFixture]
    public class HlaSearchingLookupLookupTests
    {
        private const Locus DefaultLocus = Locus.A;
        private const string CacheKey = "NmdpCodeLookup_A";

        private IHlaMatchingLookupService lookupService;
        private IHlaServiceClient hlaServiceClient;
        private IAppCache appCache;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            lookupService = DependencyInjection.DependencyInjection.Provider.GetService<IHlaMatchingLookupService>();
            hlaServiceClient = DependencyInjection.DependencyInjection.Provider.GetService<IHlaServiceClient>();
            appCache = DependencyInjection.DependencyInjection.Provider.GetService<IPersistentCacheProvider>().Cache;
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
        public void GetHlaLookupResult_WhenInvalidHlaTyping_ThrowsException()
        {
            const string hlaName = "XYZ:123:INVALID";

            Assert.ThrowsAsync<HlaMetadataDictionaryException>(
                async () => await lookupService.GetHlaLookupResult(DefaultLocus, hlaName, null));
        }

        [Test]
        public void GetHlaLookupResult_WhenNmdpCodeContainsAlleleNotInHlaMetadataDictionary_ThrowsException()
        {
            const string missingAllele = "9999:9999";

            // NMDP code value does not matter, but does need to conform to the expected pattern
            const string nmdpCode = "99:CODE";
            hlaServiceClient
                .GetAllelesForDefinedNmdpCode(DefaultLocus, nmdpCode)
                .Returns(new List<string> { missingAllele });

            Assert.ThrowsAsync<HlaMetadataDictionaryException>(async () => 
                await lookupService.GetHlaLookupResult(DefaultLocus, nmdpCode, null));
        }

        [Test]
        public void GetHlaLookupResult_WhenAlleleStringOfNamesContainsAlleleNotInHlaMetadataDictionary_ThrowsException()
        {
            const string existingAllele = "01:133";
            const string missingAllele = "9999:9999";
            const string alleleString = existingAllele + "/" + missingAllele;

            Assert.ThrowsAsync<HlaMetadataDictionaryException>(async () =>
                await lookupService.GetHlaLookupResult(DefaultLocus, alleleString, null));
        }

        [Test]
        public void GetHlaLookupResult_WhenAlleleStringOfSubtypesContainsAlleleNotInHlaMetadataDictionary_ThrowsException()
        {
            const string alleleString = "01:133/9999";

            Assert.ThrowsAsync<HlaMetadataDictionaryException>(async () =>
                await lookupService.GetHlaLookupResult(DefaultLocus, alleleString, null));
        }
    }
}
