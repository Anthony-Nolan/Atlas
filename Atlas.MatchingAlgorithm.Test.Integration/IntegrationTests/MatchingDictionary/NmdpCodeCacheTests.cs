using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.Caching;
using Atlas.Common.GeneticData;
using Atlas.MultipleAlleleCodeDictionary;
using Atlas.MultipleAlleleCodeDictionary.HLAService;
using FluentAssertions;
using LazyCache;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests.HlaMetadataDictionary
{
    [TestFixture]
    public class NmdpCodeCacheTests
    {
        private const string CacheKeyPrefix = "NmdpCodeLookup_";
        private const string NmdpCode = "*99:NMDP";
        private const string FirstAllele = "99:99";
        private const string SecondAllele = "100:100";
        private const string ExistingHlaName = FirstAllele + "/" + SecondAllele;
        private static readonly List<string> Alleles = new List<string> { FirstAllele, SecondAllele };
        private const Locus DefaultLocus = Locus.A;

        private INmdpCodeCache nmdpCodeCache;

        private IAppCache appCache;
        private IHlaServiceClient hlaServiceClient;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            nmdpCodeCache = DependencyInjection.DependencyInjection.Provider.GetService<INmdpCodeCache>();
            appCache = DependencyInjection.DependencyInjection.Provider.GetService<IPersistentCacheProvider>().Cache;
            hlaServiceClient = DependencyInjection.DependencyInjection.Provider.GetService<IHlaServiceClient>();
        }

        [SetUp]
        public void SetUp()
        {
            hlaServiceClient.GetAntigens(Arg.Any<Locus>())
                .Returns(new List<Antigen>
                {
                    new Antigen
                    {
                        Locus = DefaultLocus,
                        NmdpString = NmdpCode,
                        HlaName = ExistingHlaName
                    }
                });

            appCache.Remove($"{CacheKeyPrefix}{DefaultLocus}");
        }

        [TearDown]
        public void TearDown()
        {
            hlaServiceClient.ClearReceivedCalls();
        }

        [Test]
        public void GetOrAddAllelesForNmdpCode_WhenSubmittedValueIsNotNmdpCode_ThrowsException()
        {
            const string hlaName = "not-an-nmdp-code";

            Assert.ThrowsAsync<ArgumentException>(async () =>
                await nmdpCodeCache.GetOrAddAllelesForNmdpCode(DefaultLocus, hlaName));
        }

        [Test]
        public async Task GetOrAddAllelesForNmdpCode_CreatesCacheForLocus()
        {
            await nmdpCodeCache.GetOrAddAllelesForNmdpCode(DefaultLocus, NmdpCode);

            var locusCache = await appCache.GetAsync<Dictionary<string, IEnumerable<string>>>($"{CacheKeyPrefix}{DefaultLocus}");
            locusCache.Should().NotBeNull();
        }

        [Test]
        public async Task GetOrAddAllelesForNmdpCode_GetsAntigensFromHlaService()
        {
            await nmdpCodeCache.GetOrAddAllelesForNmdpCode(DefaultLocus, NmdpCode);

            await hlaServiceClient.Received().GetAntigens(DefaultLocus);
        }

        [Test]
        public async Task GetOrAddAllelesForNmdpCode_DoesNotCacheXxCodes()
        {
            const string xxCode = "999:XX";

            hlaServiceClient.GetAntigens(Arg.Any<Locus>())
                .Returns(new List<Antigen>
                {
                    new Antigen
                    {
                        Locus = DefaultLocus,
                        NmdpString = xxCode
                    }
                });

            await nmdpCodeCache.GetOrAddAllelesForNmdpCode(DefaultLocus, NmdpCode);

            var locusCache = await appCache.GetAsync<Dictionary<string, IEnumerable<string>>>($"{CacheKeyPrefix}{DefaultLocus}");
            locusCache.Should().NotContainKey(xxCode);
        }

        [Test]
        public async Task GetOrAddAllelesForNmdpCode_WhenNmdpStringIsInvalidHlaName_DoesNotThrowException()
        {
            const string invalidHlaName = "999:999ABC";

            hlaServiceClient.GetAntigens(Arg.Any<Locus>())
                .Returns(new List<Antigen>
                {
                    new Antigen
                    {
                        Locus = DefaultLocus,
                        NmdpString = invalidHlaName
                    }
                });

            await nmdpCodeCache.GetOrAddAllelesForNmdpCode(DefaultLocus, NmdpCode);

            Assert.DoesNotThrowAsync(async () => 
                await appCache.GetAsync<Dictionary<string, IEnumerable<string>>>($"{CacheKeyPrefix}{DefaultLocus}"));
        }

        [Test]
        public async Task GetOrAddAllelesForNmdpCode_WhenNmdpStringIsInvalidHlaName_DoesNotCacheInvalidHla()
        {
            const string invalidHlaName = "999:999ABC";

            hlaServiceClient.GetAntigens(Arg.Any<Locus>())
                .Returns(new List<Antigen>
                {
                    new Antigen
                    {
                        Locus = DefaultLocus,
                        NmdpString = invalidHlaName
                    }
                });

            await nmdpCodeCache.GetOrAddAllelesForNmdpCode(DefaultLocus, NmdpCode);

            var locusCache = await appCache.GetAsync<Dictionary<string, IEnumerable<string>>>($"{CacheKeyPrefix}{DefaultLocus}");
            locusCache.Should().NotContainKey(invalidHlaName);
        }

        [Test]
        public async Task GetOrAddAllelesForNmdpCode_DoesNotCacheNonNmdpStringAntigens()
        {
            const string nonNmdpCode = "hla-name";

            hlaServiceClient.GetAntigens(Arg.Any<Locus>())
                .Returns(new List<Antigen>
                {
                    new Antigen
                    {
                        Locus = DefaultLocus,
                        HlaName = nonNmdpCode
                    }
                });

            await nmdpCodeCache.GetOrAddAllelesForNmdpCode(DefaultLocus, NmdpCode);

            var locusCache = await appCache.GetAsync<Dictionary<string, IEnumerable<string>>>($"{CacheKeyPrefix}{DefaultLocus}");
            locusCache.Should().NotContainKey(nonNmdpCode);
            locusCache.Values.Should().NotContain(nonNmdpCode);
        }

        [TestCase("99:99/100:100/")]
        [TestCase("invalid-string")]
        public void GetOrAddAllelesForNmdpCode_WhenAlleleStringNotValid_DoesNotThrowException(string invalidAlleleString)
        {
            hlaServiceClient.GetAntigens(Arg.Any<Locus>())
                .Returns(new List<Antigen>
                {
                    new Antigen
                    {
                        Locus = DefaultLocus,
                        NmdpString = NmdpCode,
                        HlaName = invalidAlleleString
                    }
                });

            Assert.DoesNotThrowAsync(async () =>
                await nmdpCodeCache.GetOrAddAllelesForNmdpCode(DefaultLocus, NmdpCode));
        }

        [TestCase("99:99/100:100/")]
        [TestCase("invalid-string")]
        public async Task GetOrAddAllelesForNmdpCode_WhenAlleleStringNotValid_DoesNotCacheNmdpCode(string invalidAlleleString)
        {
            const string codeWithInvalidAlleleString = "*99:INVALID";

            hlaServiceClient.GetAntigens(Arg.Any<Locus>())
                .Returns(new List<Antigen>
                {
                    new Antigen
                    {
                        Locus = DefaultLocus,
                        NmdpString = codeWithInvalidAlleleString,
                        HlaName = invalidAlleleString
                    }
                });

            await nmdpCodeCache.GetOrAddAllelesForNmdpCode(DefaultLocus, NmdpCode);

            var locusCache = await appCache.GetAsync<Dictionary<string, IEnumerable<string>>>($"{CacheKeyPrefix}{DefaultLocus}");
            locusCache.Should().NotContainKey(codeWithInvalidAlleleString);
        }

        [Test]
        public async Task GetOrAddAllelesForNmdpCode_WhenCacheExistsForLocus_DoesNotGetAntigensFromHlaService()
        {
            var locusCache = new Dictionary<string, IEnumerable<string>>
            {
                { NmdpCode, Alleles }
            };
            appCache.Add($"{CacheKeyPrefix}{DefaultLocus}", locusCache);

            await nmdpCodeCache.GetOrAddAllelesForNmdpCode(DefaultLocus, NmdpCode);

            await hlaServiceClient.DidNotReceive().GetAntigens(DefaultLocus);
        }

        [Test]
        public async Task GetOrAddAllelesForNmdpCode_NmdpCodeExistsInCache_DoesNotGetAllelesFromHlaService()
        {
            await nmdpCodeCache.GetOrAddAllelesForNmdpCode(DefaultLocus, NmdpCode);

            await hlaServiceClient.DidNotReceive()
                .GetAllelesForDefinedNmdpCode(Arg.Any<Locus>(), NmdpCode);
        }

        [Test]
        public async Task GetOrAddAllelesForNmdpCode_NmdpCodeExistsInCache_ReturnsExpectedAlleles()
        {
            var alleles = await nmdpCodeCache.GetOrAddAllelesForNmdpCode(DefaultLocus, NmdpCode);

            alleles.Should().BeEquivalentTo(Alleles);
        }

        [Test]
        public async Task GetOrAddAllelesForNmdpCode_NmdpCodeDoesNotExistInCache_GetsAllelesFromHlaService()
        {
            const string newNmdpCode = "*99:NEW";

            await nmdpCodeCache.GetOrAddAllelesForNmdpCode(DefaultLocus, newNmdpCode);

            await hlaServiceClient.Received().GetAllelesForDefinedNmdpCode(Locus.A, newNmdpCode);
        }

        [Test]
        public async Task GetOrAddAllelesForNmdpCode_NmdpCodeDoesNotExistInCache_ReturnsExpectedAlleles()
        {
            const string newNmdpCode = "*99:NEW";
            var expectedAlleles = new List<string> { "allele-1", "allele-2" };

            hlaServiceClient
                .GetAllelesForDefinedNmdpCode(Arg.Any<Locus>(), newNmdpCode)
                .Returns(expectedAlleles);

            var alleles = await nmdpCodeCache.GetOrAddAllelesForNmdpCode(DefaultLocus, newNmdpCode);

            alleles.Should().BeEquivalentTo(expectedAlleles);
        }

        [Test]
        public async Task GetOrAddAllelesForNmdpCode_NmdpCodeDoesNotExistInCache_UpdatesCacheWithNewCode()
        {
            const string newNmdpCode = "*99:NEW";
            var expectedAlleles = new List<string> { "allele-1", "allele-2" };

            hlaServiceClient
                .GetAllelesForDefinedNmdpCode(Arg.Any<Locus>(), newNmdpCode)
                .Returns(expectedAlleles);

            await nmdpCodeCache.GetOrAddAllelesForNmdpCode(DefaultLocus, newNmdpCode);

            var locusCache = await appCache.GetAsync<Dictionary<string, IEnumerable<string>>>($"{CacheKeyPrefix}{DefaultLocus}");
            locusCache.Should().ContainKey(newNmdpCode);
            locusCache.GetValueOrDefault(newNmdpCode).Should().BeEquivalentTo(expectedAlleles);
        }
    }
}
