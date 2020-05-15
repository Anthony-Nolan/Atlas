using FluentAssertions;
using LazyCache;
using Microsoft.Extensions.DependencyInjection;
using Atlas.HLAService.Client;
using Atlas.HlaMetadataDictionary.Services;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Utils.Caching;
using Atlas.Utils.Core.Models;

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests.MatchingDictionary
{
    /// <summary>
    /// Fixture relies on a file-backed matching dictionary - tests may break if underlying data is changed.
    /// </summary>
    [TestFixture]
    public class Dpb1TceGroupLookupTests
    {
        private const LocusType Dpb1MolecularLocusType = LocusType.Dpb1;
        private const string CacheKey = "NmdpCodeLookup_Dpb1";

        private IDpb1TceGroupLookupService lookupService;
        private IHlaServiceClient hlaServiceClient;
        private IAppCache appCache;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            lookupService = DependencyInjection.DependencyInjection.Provider.GetService<IDpb1TceGroupLookupService>();
            hlaServiceClient = DependencyInjection.DependencyInjection.Provider.GetService<IHlaServiceClient>();
            appCache = DependencyInjection.DependencyInjection.Provider.GetService<IPersistentCacheProvider>().Cache;
        }

        [SetUp]
        public void SetUp()
        {
            hlaServiceClient
                .GetAllelesForDefinedNmdpCode(Dpb1MolecularLocusType, Arg.Any<string>())
                .Returns(new List<string>());

            // clear NMDP code allele mappings between tests
            appCache.Remove(CacheKey);
        }

        [Test]
        public async Task GetDpb1TceGroup_WhenNmdpCodeMapsToSingleTceGroup_ReturnsTceGroup()
        {
            // both alleles map to same TCE group
            const string firstAllele = "02:01";
            const string secondAllele = "02:02";
            const string tceGroup = "3";

            // NMDP code value does not matter, but does need to conform to the expected pattern
            const string nmdpCode = "99:CODE";
            hlaServiceClient
                .GetAllelesForDefinedNmdpCode(Dpb1MolecularLocusType, nmdpCode)
                .Returns(new List<string> { firstAllele, secondAllele });

            var result = await lookupService.GetDpb1TceGroup(nmdpCode, null);

            result.Should().Be(tceGroup);
        }

        [Test]
        public async Task GetDpb1TceGroup_WhenNmdpCodeMapsToMoreThanOneTceGroup_DoesNotReturnTceGroup()
        {
            // alleles map to different TCE groups
            const string firstAllele = "02:01";
            const string secondAllele = "03:01";

            // NMDP code value does not matter, but does need to conform to the expected pattern
            const string nmdpCode = "99:CODE";
            hlaServiceClient
                .GetAllelesForDefinedNmdpCode(Dpb1MolecularLocusType, nmdpCode)
                .Returns(new List<string> { firstAllele, secondAllele });

            var result = await lookupService.GetDpb1TceGroup(nmdpCode, null);

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

            var result = await lookupService.GetDpb1TceGroup(alleleString, null);

            result.Should().Be(tceGroup);
        }

        [Test]
        public async Task GetDpb1TceGroup_WhenAlleleStringOfSubtypesMapsToSingleTceGroup_ReturnsTceGroup()
        {
            // both alleles map to same TCE group
            const string tceGroup = "3";
            const string alleleString = "02:01/02";

            var result = await lookupService.GetDpb1TceGroup(alleleString, null);

            result.Should().Be(tceGroup);
        }

        [Test]
        public async Task GetDpb1TceGroup_WhenAlleleStringOfNamesMapsToMultipleTceGroups_DoesNotReturnTceGroup()
        {
            // alleles map to different TCE group
            const string firstAllele = "02:01";
            const string secondAllele = "03:01";
            const string alleleString = firstAllele + "/" + secondAllele;

            var result = await lookupService.GetDpb1TceGroup(alleleString, null);

            result.Should().BeNullOrEmpty();
        }
    }
}